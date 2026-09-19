using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Domain.Raster;
using ForestProof.Backend.Domain.Spectral;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.ChangeZones.Interfaces;
using ForestProof.Backend.Services.Raster;
using ForestProof.Backend.Services.Raster.Interfaces;
using ForestProof.Backend.Services.Spectral.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.ChangeZones;

/// <summary>
/// Сопоставляет зонам изменений подтверждающие наблюдения и статус причины.
/// </summary>
/// <param name="rasterService">Сэмплирование растров MODIS и Sentinel-2.</param>
/// <param name="spectralIndexService">Расчёт dNBR по сценам Sentinel-2.</param>
/// <param name="sceneSelector">Выбор пары сцен Sentinel-2.</param>
/// <param name="options">Порог dNBR.</param>
public sealed class ChangeZoneEvidenceAnalyzer(
    IRasterService rasterService,
    ISpectralIndexService spectralIndexService,
    ISentinelSceneSelector sceneSelector,
    IOptions<CalculationOptions> options) : IChangeZoneEvidenceAnalyzer
{
    private readonly CalculationOptions _options = options.Value;

    /// <inheritdoc />
    public IReadOnlyList<ChangeZoneEvidence> Analyze(
        string aoiId,
        IReadOnlyList<ChangeZone> zones,
        RasterGrid agbGrid,
        GfcWindow gfc,
        int startYear,
        int endYear)
    {
        var scenePair = sceneSelector.SelectPair(aoiId, startYear, endYear);
        var dnbr = ComputeDnbr(aoiId, scenePair);

        var results = new List<ChangeZoneEvidence>();

        foreach (var zone in zones)
        {
            var lossYears = zone.Pixels
                .Select(pixel => GfcSampler.GetLossYear(agbGrid, pixel.Row, pixel.Column, gfc))
                .Where(year => year.HasValue)
                .Select(year => year!.Value)
                .Distinct()
                .Order()
                .ToArray();

            var evidenceTypes = new List<EvidenceType>();
            var evidenceYears = new List<int>();

            if (lossYears.Length > 0)
            {
                evidenceTypes.Add(EvidenceType.Gfc);
                evidenceYears.AddRange(lossYears);
            }

            if (HasModisBurn(zone, agbGrid, aoiId))
            {
                evidenceTypes.Add(EvidenceType.Modis);
                if (rasterService.GetModisBurnYear(aoiId) is { } modisYear)
                    evidenceYears.Add(modisYear);
            }

            if (scenePair is not null &&
                dnbr is { } dnbrWindow &&
                HasSentinelEvidence(zone, agbGrid, aoiId, scenePair, dnbrWindow))
            {
                evidenceTypes.Add(EvidenceType.Sentinel2);
                evidenceYears.Add(scenePair.AfterYear);
            }

            results.Add(new ChangeZoneEvidence
            {
                ZoneId = zone.Id,
                EvidenceTypes = evidenceTypes,
                GfcLossYears = lossYears,
                EvidenceYears = evidenceYears.Distinct().Order().ToArray(),
                CauseStatus = CauseStatusRules.Evaluate(evidenceTypes, evidenceYears, startYear, endYear)
            });
        }

        return results;
    }

    private (IReadOnlyList<double?> Values, RasterGrid Grid)? ComputeDnbr(string aoiId, SentinelScenePair? scenePair)
    {
        if (scenePair is null)
            return null;

        var before = spectralIndexService.Calculate(
            aoiId,
            scenePair.BeforeReflectanceFileName,
            scenePair.BeforeSclFileName);
        var after = spectralIndexService.Calculate(
            aoiId,
            scenePair.AfterReflectanceFileName,
            scenePair.AfterSclFileName);

        return (spectralIndexService.CalculateDnbr(before, after), before.Grid);
    }

    private bool HasSentinelEvidence(
        ChangeZone zone,
        RasterGrid agbGrid,
        string aoiId,
        SentinelScenePair scenePair,
        (IReadOnlyList<double?> Values, RasterGrid Grid) dnbr)
    {
        foreach (var pixel in zone.Pixels)
        {
            var longitude = agbGrid.OriginLongitude + (pixel.Column + 0.5) * agbGrid.PixelWidthDegrees;
            var latitude = agbGrid.OriginLatitude - (pixel.Row + 0.5) * agbGrid.PixelHeightDegrees;

            if (rasterService.TransformToPixel(aoiId, scenePair.BeforeReflectanceFileName, longitude, latitude)
                is not { } position)
                continue;

            var index = position.Row * dnbr.Grid.Width + position.Column;
            if (index < 0 || index >= dnbr.Values.Count)
                continue;

            if (dnbr.Values[index] is { } value && value > _options.SentinelDnbrThreshold)
                return true;
        }

        return false;
    }

    private bool HasModisBurn(ChangeZone zone, RasterGrid agbGrid, string aoiId)
    {
        foreach (var pixel in zone.Pixels)
        {
            var longitude = agbGrid.OriginLongitude + (pixel.Column + 0.5) * agbGrid.PixelWidthDegrees;
            var latitude = agbGrid.OriginLatitude - (pixel.Row + 0.5) * agbGrid.PixelHeightDegrees;

            if (rasterService.SampleModisBurnDate(aoiId, longitude, latitude) is > 0)
                return true;
        }

        return false;
    }
}
