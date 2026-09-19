using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Domain.Baseline;
using ForestProof.Backend.Domain.Carbon;
using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Domain.Enums;
using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Domain.Pixels;
using ForestProof.Backend.Domain.Raster;
using ForestProof.Backend.Domain.Units;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Analysis.Interfaces;
using ForestProof.Backend.Services.Calculation.Interfaces;
using ForestProof.Backend.Services.ChangeZones.Interfaces;
using ForestProof.Backend.Services.Data.Interfaces;
using ForestProof.Backend.Services.Geometry.Interfaces;
using ForestProof.Backend.Services.Provenance.Interfaces;
using ForestProof.Backend.Services.Raster;
using ForestProof.Backend.Services.Raster.Interfaces;
using Microsoft.Extensions.Options;
using NetTopologySuite.IO;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace ForestProof.Backend.Services.Analysis;

/// <summary>
/// Выполняет полный расчёт по территории и периоду.
/// </summary>
/// <param name="aoiCatalogReader">Каталог территорий и геометрия.</param>
/// <param name="baselineReader">Методическая базовая линия.</param>
/// <param name="rasterService">Растры биомассы, GFC, MODIS, Sentinel-2 и CCI Change.</param>
/// <param name="geometryService">Площади пересечения пикселей с полигоном.</param>
/// <param name="carbonCalculator">Запас и изменение углерода.</param>
/// <param name="uncertaintyCalculator">Сценарный диапазон неопределённости.</param>
/// <param name="baselineCalculator">Базовая линия.</param>
/// <param name="unitCalculator">Потенциальные единицы.</param>
/// <param name="changeZoneDetector">Выделение зон изменений.</param>
/// <param name="changeZoneEvidenceAnalyzer">Доказательства и статус причины зон.</param>
/// <param name="options">Параметры методики.</param>
public sealed class AnalysisPipeline(
    IAoiCatalogReader aoiCatalogReader,
    IBaselineReader baselineReader,
    IRasterService rasterService,
    IGeometryService geometryService,
    ICarbonCalculator carbonCalculator,
    IUncertaintyCalculator uncertaintyCalculator,
    IBaselineCalculator baselineCalculator,
    IUnitCalculator unitCalculator,
    IChangeZoneDetector changeZoneDetector,
    IChangeZoneEvidenceAnalyzer changeZoneEvidenceAnalyzer,
    ISourceCatalogService sourceCatalogService,
    IOptions<CalculationOptions> options) : IAnalysisPipeline
{
    private const double CoverageTolerance = 1e-9;

    private readonly CalculationOptions _options = options.Value;

    /// <inheritdoc />
    public AnalysisSummary Run(AnalysisRequest request)
    {
        ValidateRequest(request);

        var requestPolygon = ResolveRequestPolygon(request);
        var areas = aoiCatalogReader.ReadAreas();

        var warnings = new List<string>();
        var parts = new List<(string AoiId, BaselineRecord Baseline, PixelAreaResult PixelAreas)>();

        foreach (var area in areas)
        {
            var aoiGeometry = aoiCatalogReader.ReadGeometry(area.Id);
            if (!aoiGeometry.Intersects(requestPolygon))
                continue;

            var partPolygon = aoiGeometry.Intersection(requestPolygon);
            if (partPolygon.IsEmpty)
                continue;

            var startWindow = rasterService.ReadBiomass(area.Id, request.StartYear);
            var pixelAreas = geometryService.CalculatePixelAreas(partPolygon, startWindow.Grid);
            if (pixelAreas.Pixels.Count == 0)
                continue;

            warnings.AddRange(pixelAreas.Warnings);
            parts.Add((area.Id, baselineReader.ReadBaseline(area.Id), pixelAreas));
        }

        if (parts.Count == 0)
            throw new InvalidOperationException("Запрос не пересекает покрытие данных.");

        var polygonAreaHectares = parts.Sum(part => part.PixelAreas.PolygonAreaHectares);
        if (polygonAreaHectares / 100.0 > _options.MaxAreaSquareKilometers)
            throw new InvalidOperationException("Площадь запроса больше допустимой.");

        var years = Enumerable
            .Range(request.StartYear, request.EndYear - request.StartYear + 1)
            .ToArray();

        var totalCarbonByYear = new Dictionary<int, double>();
        var validAreaByYear = new Dictionary<int, double>();
        var standardDeviationAreaByYear = new Dictionary<int, double>();
        var startSamples = new List<PixelSample>();
        var endSamples = new List<PixelSample>();
        var changeZones = new List<ChangeZone>();
        var changeZoneEvidence = new List<ChangeZoneEvidence>();
        var nextZoneId = 1;

        foreach (var part in parts)
        {
            var windows = new Dictionary<int, BiomassWindow>();

            foreach (var year in years)
            {
                var window = rasterService.ReadBiomass(part.AoiId, year);
                windows[year] = window;

                var samples = AssembleSamples(part.PixelAreas.Pixels, window);
                var stock = carbonCalculator.AggregateCarbonStock(samples);

                totalCarbonByYear[year] = totalCarbonByYear.GetValueOrDefault(year) + stock.TotalCarbon;
                validAreaByYear[year] = validAreaByYear.GetValueOrDefault(year) + stock.AreaHectares;
                standardDeviationAreaByYear[year] = standardDeviationAreaByYear.GetValueOrDefault(year) +
                    samples.Where(sample => sample.StandardDeviation.HasValue).Sum(sample => sample.AreaHectares);

                if (year == request.StartYear)
                    startSamples.AddRange(samples);

                if (year == request.EndYear)
                    endSamples.AddRange(samples);
            }

            var gfc = rasterService.ReadGfc(part.AoiId);
            var partZones = changeZoneDetector.Detect(
                BuildChangePixels(
                    part.PixelAreas.Pixels,
                    windows[request.StartYear],
                    windows[request.EndYear],
                    gfc),
                windows[request.StartYear].Grid);
            var partEvidence = changeZoneEvidenceAnalyzer.Analyze(
                part.AoiId,
                partZones,
                windows[request.StartYear].Grid,
                gfc,
                request.StartYear,
                request.EndYear,
                request.UseExtendedSclClasses);

            var idMap = new Dictionary<int, int>();
            foreach (var zone in partZones)
            {
                var newId = nextZoneId++;
                idMap[zone.Id] = newId;
                changeZones.Add(zone with { Id = newId });
            }

            foreach (var evidence in partEvidence)
                changeZoneEvidence.Add(evidence with { ZoneId = idMap[evidence.ZoneId] });
        }

        var series = years.Select(year =>
        {
            var areaHectares = validAreaByYear[year];
            var coverage = polygonAreaHectares > 0 ? areaHectares / polygonAreaHectares : 0;
            var standardDeviationCoverage = polygonAreaHectares > 0
                ? standardDeviationAreaByYear[year] / polygonAreaHectares
                : 0;

            return new YearlyCarbonStock
            {
                Year = year,
                AreaHectares = areaHectares,
                TotalCarbon = totalCarbonByYear[year],
                MeanCarbonPerHectare = areaHectares > 0 ? totalCarbonByYear[year] / areaHectares : 0,
                Coverage = coverage,
                HasCompleteCoverage = coverage >= 1 - CoverageTolerance &&
                                      standardDeviationCoverage >= 1 - CoverageTolerance
            };
        }).ToList();

        var change = carbonCalculator.CalculateChange(
            ToCarbonStock(series[0]),
            ToCarbonStock(series[^1]),
            request.StartYear,
            request.EndYear);

        var range = uncertaintyCalculator.Calculate(
            startSamples,
            endSamples,
            change.ProjectEmission,
            request.SensitivityCoefficient ?? _options.DefaultSensitivityCoefficient);

        var baselineResult = AggregateBaseline(parts, request.StartYear, request.EndYear);

        var hasCompleteCoverage = series[0].HasCompleteCoverage && series[^1].HasCompleteCoverage;
        if (!hasCompleteCoverage)
            warnings.Add("Обязательное покрытие неполное: число единиц недоступно.");

        var intersectingPixelCount = parts.Sum(part => part.PixelAreas.Pixels.Count);
        var hasMandatoryData = startSamples.Count == intersectingPixelCount &&
                               endSamples.Count == intersectingPixelCount;

        var units = unitCalculator.Calculate(new UnitCalculationInput
        {
            AreaHectares = series[^1].AreaHectares,
            StartYear = request.StartYear,
            EndYear = request.EndYear,
            HasMandatoryData = hasMandatoryData,
            HasCompleteCoverage = hasCompleteCoverage,
            HasBaseline = true,
            BaselineEmission = baselineResult.BaselineEmission,
            ProjectEmission = change.ProjectEmission,
            Uncertainty = range.HalfWidth,
            Leakage = _options.LeakageTonnesCo2
        });

        var status = units.Status == UnitStatus.Unavailable
            ? RunStatus.UnitsUnavailable
            : warnings.Count > 0
                ? RunStatus.Partial
                : RunStatus.Complete;

        var sensitivityCoefficient = request.SensitivityCoefficient ?? _options.DefaultSensitivityCoefficient;
        var sourceAssets = parts
            .SelectMany(part => sourceCatalogService.ReadCatalog(part.AoiId))
            .GroupBy(asset => asset.RelativePath)
            .Select(group => group.First())
            .ToArray();

        return new AnalysisSummary
        {
            RunId = Guid.NewGuid().ToString("N"),
            MethodVersion = _options.MethodVersion,
            DataVersion = _options.DataVersion,
            CreatedAt = DateTimeOffset.UtcNow,
            InputHash = AnalysisInputHash.Compute(request, sensitivityCoefficient),
            SourceAssets = sourceAssets,
            Status = status,
            AoiId = request.AoiId ?? string.Empty,
            StartYear = request.StartYear,
            EndYear = request.EndYear,
            PolygonAreaHectares = polygonAreaHectares,
            YearlySeries = series,
            Change = change,
            Uncertainty = range,
            Baseline = baselineResult,
            Units = units,
            ChangeZones = changeZones,
            ChangeZoneEvidence = changeZoneEvidence,
            CciChangeMeanTonnesPerHectare = ComputeCciChangeMean(parts),
            Warnings = warnings
        };
    }

    private NtsGeometry ResolveRequestPolygon(AnalysisRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.PolygonGeoJson))
            return ParseGeoJson(request.PolygonGeoJson);

        if (string.IsNullOrWhiteSpace(request.AoiId))
            throw new ArgumentException("Укажите AoiId или PolygonGeoJson.", nameof(request));

        return aoiCatalogReader.ReadGeometry(request.AoiId);
    }

    private static NtsGeometry ParseGeoJson(string geoJson)
    {
        var reader = new GeoJsonReader();

        using var document = System.Text.Json.JsonDocument.Parse(geoJson);
        var type = document.RootElement.GetProperty("type").GetString();

        return type switch
        {
            "FeatureCollection" => reader.Read<NetTopologySuite.Features.FeatureCollection>(geoJson)
                .First().Geometry,
            "Feature" => reader.Read<NetTopologySuite.Features.Feature>(geoJson).Geometry,
            _ => reader.Read<NtsGeometry>(geoJson)
        };
    }

    private BaselineResult AggregateBaseline(
        IReadOnlyList<(string AoiId, BaselineRecord Baseline, PixelAreaResult PixelAreas)> parts,
        int startYear,
        int endYear)
    {
        var baselineEmission = 0.0;
        var weightedStart = 0.0;
        var weightedEnd = 0.0;
        var weightedRate = 0.0;
        var totalArea = 0.0;

        foreach (var part in parts)
        {
            var result = baselineCalculator.Calculate(
                part.Baseline.ReferenceMean2015,
                part.Baseline.ReferenceMean2019,
                part.PixelAreas.PolygonAreaHectares,
                startYear,
                endYear);

            baselineEmission += result.BaselineEmission;

            var area = part.PixelAreas.PolygonAreaHectares;
            weightedStart += result.StartCarbonPerHectare * area;
            weightedEnd += result.EndCarbonPerHectare * area;
            weightedRate += result.HistoricalRate * area;
            totalArea += area;
        }

        return new BaselineResult
        {
            HistoricalRate = totalArea > 0 ? weightedRate / totalArea : 0,
            StartCarbonPerHectare = totalArea > 0 ? weightedStart / totalArea : 0,
            EndCarbonPerHectare = totalArea > 0 ? weightedEnd / totalArea : 0,
            BaselineEmission = baselineEmission
        };
    }

    private double? ComputeCciChangeMean(
        IReadOnlyList<(string AoiId, BaselineRecord Baseline, PixelAreaResult PixelAreas)> parts)
    {
        var weightedDifference = 0.0;
        var totalArea = 0.0;

        foreach (var part in parts)
        {
            var window = rasterService.ReadChange(part.AoiId);
            var width = window.Grid.Width;

            foreach (var pixel in part.PixelAreas.Pixels)
            {
                var index = pixel.Row * width + pixel.Column;
                if (index < 0 || index >= window.AgbDifference.Count)
                    continue;

                if (window.AgbDifference[index] is not { } difference)
                    continue;

                weightedDifference += difference * pixel.AreaHectares;
                totalArea += pixel.AreaHectares;
            }
        }

        return totalArea > 0 ? weightedDifference / totalArea : null;
    }

    private void ValidateRequest(AnalysisRequest request)
    {
        if (request.StartYear >= request.EndYear)
            throw new ArgumentException("Начальный год должен быть меньше конечного.", nameof(request));

        if (request.StartYear < _options.MinAnalysisYear || request.EndYear > _options.MaxAnalysisYear)
            throw new ArgumentOutOfRangeException(
                nameof(request),
                $"Период должен быть в диапазоне {_options.MinAnalysisYear}–{_options.MaxAnalysisYear}.");
    }

    private static IReadOnlyList<PixelSample> AssembleSamples(
        IReadOnlyList<PixelIntersection> pixels,
        BiomassWindow window)
    {
        var samples = new List<PixelSample>();

        foreach (var pixel in pixels)
        {
            var index = pixel.Row * window.Grid.Width + pixel.Column;
            if (index < 0 || index >= window.Biomass.Count)
                continue;

            if (window.Biomass[index] is not { } biomass)
                continue;

            samples.Add(new PixelSample
            {
                Biomass = biomass,
                AreaHectares = pixel.AreaHectares,
                StandardDeviation = window.StandardDeviation[index]
            });
        }

        return samples;
    }

    private static IReadOnlyList<ChangePixel> BuildChangePixels(
        IReadOnlyList<PixelIntersection> pixels,
        BiomassWindow startWindow,
        BiomassWindow endWindow,
        GfcWindow gfc)
    {
        var width = startWindow.Grid.Width;
        var changePixels = new List<ChangePixel>();

        foreach (var pixel in pixels)
        {
            var index = pixel.Row * width + pixel.Column;
            if (index < 0 || index >= startWindow.Biomass.Count || index >= endWindow.Biomass.Count)
                continue;

            if (startWindow.Biomass[index] is not { } startBiomass)
                continue;

            if (endWindow.Biomass[index] is not { } endBiomass)
                continue;

            changePixels.Add(new ChangePixel
            {
                Row = pixel.Row,
                Column = pixel.Column,
                AreaHectares = pixel.AreaHectares,
                BiomassChange = endBiomass - startBiomass,
                HasConfirmation = GfcSampler.HasLoss(startWindow.Grid, pixel.Row, pixel.Column, gfc)
            });
        }

        return changePixels;
    }

    private static CarbonStock ToCarbonStock(YearlyCarbonStock stock) => new()
    {
        AreaHectares = stock.AreaHectares,
        TotalCarbon = stock.TotalCarbon,
        MeanCarbonPerHectare = stock.MeanCarbonPerHectare
    };
}
