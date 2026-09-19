using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Domain.Raster;
using ForestProof.Backend.Services.ChangeZones.Interfaces;

namespace ForestProof.Backend.Services.ChangeZones;

/// <summary>
/// Сопоставляет зонам изменений подтверждающие наблюдения и статус причины.
/// </summary>
public sealed class ChangeZoneEvidenceAnalyzer : IChangeZoneEvidenceAnalyzer
{
    /// <inheritdoc />
    public IReadOnlyList<ChangeZoneEvidence> Analyze(
        IReadOnlyList<ChangeZone> zones,
        RasterGrid agbGrid,
        GfcWindow gfc)
    {
        var results = new List<ChangeZoneEvidence>();

        foreach (var zone in zones)
        {
            var evidenceTypes = new List<EvidenceType>();
            if (HasGfcLoss(zone, agbGrid, gfc))
                evidenceTypes.Add(EvidenceType.Gfc);

            results.Add(new ChangeZoneEvidence
            {
                ZoneId = zone.Id,
                EvidenceTypes = evidenceTypes,
                CauseStatus = CauseStatusRules.Evaluate(evidenceTypes)
            });
        }

        return results;
    }

    private static bool HasGfcLoss(ChangeZone zone, RasterGrid agbGrid, GfcWindow gfc)
    {
        foreach (var pixel in zone.Pixels)
        {
            var longitude = agbGrid.OriginLongitude + (pixel.Column + 0.5) * agbGrid.PixelWidthDegrees;
            var latitude = agbGrid.OriginLatitude - (pixel.Row + 0.5) * agbGrid.PixelHeightDegrees;

            var column = (int)Math.Floor((longitude - gfc.Grid.OriginLongitude) / gfc.Grid.PixelWidthDegrees);
            var row = (int)Math.Floor((gfc.Grid.OriginLatitude - latitude) / gfc.Grid.PixelHeightDegrees);

            if (row < 0 || row >= gfc.Grid.Height || column < 0 || column >= gfc.Grid.Width)
                continue;

            var index = row * gfc.Grid.Width + column;
            if (gfc.LossYear[index] is > 0)
                return true;
        }

        return false;
    }
}
