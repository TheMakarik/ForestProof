using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Domain.Carbon;
using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Domain.Pixels;
using ForestProof.Backend.Domain.Raster;
using ForestProof.Backend.Domain.Units;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Analysis.Interfaces;
using ForestProof.Backend.Services.Calculation.Interfaces;
using ForestProof.Backend.Services.Data.Interfaces;
using ForestProof.Backend.Services.Geometry.Interfaces;
using ForestProof.Backend.Services.Raster.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Analysis;

/// <summary>
/// Выполняет полный расчёт по территории и периоду.
/// </summary>
/// <param name="aoiCatalogReader">Каталог территорий и геометрия.</param>
/// <param name="baselineReader">Методическая базовая линия.</param>
/// <param name="rasterService">Растры биомассы AGB и AGB_SD.</param>
/// <param name="geometryService">Площади пересечения пикселей с полигоном.</param>
/// <param name="carbonCalculator">Запас и изменение углерода.</param>
/// <param name="uncertaintyCalculator">Сценарный диапазон неопределённости.</param>
/// <param name="baselineCalculator">Базовая линия.</param>
/// <param name="unitCalculator">Потенциальные единицы.</param>
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
    IOptions<CalculationOptions> options) : IAnalysisPipeline
{
    private const double CoverageTolerance = 1e-9;

    private readonly CalculationOptions _options = options.Value;

    /// <inheritdoc />
    public AnalysisSummary Run(AnalysisRequest request)
    {
        ValidateRequest(request);

        if (aoiCatalogReader.ReadAreas().All(item => item.Id != request.AoiId))
            throw new KeyNotFoundException($"AOI '{request.AoiId}' не найден.");

        var geometry = aoiCatalogReader.ReadGeometry(request.AoiId);
        var baseline = baselineReader.ReadBaseline(request.AoiId);

        var warnings = new List<string>();

        var startWindow = rasterService.ReadBiomass(request.AoiId, request.StartYear);
        var pixelAreas = geometryService.CalculatePixelAreas(geometry, startWindow.Grid);
        warnings.AddRange(pixelAreas.Warnings);

        if (pixelAreas.PolygonAreaHectares / 100.0 > _options.MaxAreaSquareKilometers)
            throw new InvalidOperationException("Площадь запроса больше допустимой.");

        var samplesByYear = new Dictionary<int, IReadOnlyList<PixelSample>>();
        var series = new List<YearlyCarbonStock>();

        for (var year = request.StartYear; year <= request.EndYear; year++)
        {
            var window = rasterService.ReadBiomass(request.AoiId, year);
            var samples = AssembleSamples(pixelAreas.Pixels, window);
            samplesByYear[year] = samples;

            var stock = carbonCalculator.AggregateCarbonStock(samples);
            var coverage = pixelAreas.PolygonAreaHectares > 0
                ? stock.AreaHectares / pixelAreas.PolygonAreaHectares
                : 0;
            var standardDeviationCoverage = pixelAreas.PolygonAreaHectares > 0
                ? samples.Where(sample => sample.StandardDeviation.HasValue).Sum(sample => sample.AreaHectares) /
                  pixelAreas.PolygonAreaHectares
                : 0;

            series.Add(new YearlyCarbonStock
            {
                Year = year,
                AreaHectares = stock.AreaHectares,
                TotalCarbon = stock.TotalCarbon,
                MeanCarbonPerHectare = stock.MeanCarbonPerHectare,
                Coverage = coverage,
                HasCompleteCoverage = coverage >= 1 - CoverageTolerance &&
                                      standardDeviationCoverage >= 1 - CoverageTolerance
            });
        }

        var startStock = series[0];
        var endStock = series[^1];
        var change = carbonCalculator.CalculateChange(
            ToCarbonStock(startStock),
            ToCarbonStock(endStock),
            request.StartYear,
            request.EndYear);

        var range = uncertaintyCalculator.Calculate(
            samplesByYear[request.StartYear],
            samplesByYear[request.EndYear],
            change.ProjectEmission,
            _options.DefaultSensitivityCoefficient);

        var baselineResult = baselineCalculator.Calculate(
            baseline.ReferenceMean2015,
            baseline.ReferenceMean2019,
            pixelAreas.PolygonAreaHectares,
            request.StartYear,
            request.EndYear);

        var hasCompleteCoverage = startStock.HasCompleteCoverage && endStock.HasCompleteCoverage;
        if (!hasCompleteCoverage)
            warnings.Add("Обязательное покрытие неполное: число единиц недоступно.");

        var units = unitCalculator.Calculate(new UnitCalculationInput
        {
            AreaHectares = endStock.AreaHectares,
            StartYear = request.StartYear,
            EndYear = request.EndYear,
            HasMandatoryData = true,
            HasCompleteCoverage = hasCompleteCoverage,
            HasBaseline = true,
            BaselineEmission = baselineResult.BaselineEmission,
            ProjectEmission = change.ProjectEmission,
            Uncertainty = range.HalfWidth,
            Leakage = _options.LeakageTonnesCo2
        });

        return new AnalysisSummary
        {
            AoiId = request.AoiId,
            StartYear = request.StartYear,
            EndYear = request.EndYear,
            PolygonAreaHectares = pixelAreas.PolygonAreaHectares,
            YearlySeries = series,
            Change = change,
            Uncertainty = range,
            Baseline = baselineResult,
            Units = units,
            Warnings = warnings
        };
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

    private static CarbonStock ToCarbonStock(YearlyCarbonStock stock) => new()
    {
        AreaHectares = stock.AreaHectares,
        TotalCarbon = stock.TotalCarbon,
        MeanCarbonPerHectare = stock.MeanCarbonPerHectare
    };
}
