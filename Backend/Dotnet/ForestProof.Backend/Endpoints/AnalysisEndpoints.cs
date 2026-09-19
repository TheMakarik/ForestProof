using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Endpoints.Contracts;
using ForestProof.Backend.Options;
using ForestProof.Backend.Persistence.Interfaces;
using ForestProof.Backend.Services.Analysis.Interfaces;
using ForestProof.Backend.Services.Data;
using ForestProof.Backend.Services.Report.Interfaces;
using ForestProof.Backend.Services.Spectral.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Endpoints;

/// <summary>
/// Регистрирует внутренние расчётные эндпоинты REST API.
/// </summary>
public static class AnalysisEndpoints
{
    private const string SourcesFileName = "sources.csv";

    /// <summary>
    /// Добавляет группу эндпоинтов "/api/v1" для расчётов, отчётов и источников.
    /// </summary>
    /// <param name="endpoints">Построитель маршрутов приложения.</param>
    /// <returns>Тот же построитель маршрутов для цепочки вызовов.</returns>
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1");

        group.MapPost("/analyses", async (
            CreateAnalysisRequest request,
            IAnalysisPipeline pipeline,
            IAnalysisRunRepository repository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var analysisRequest = new AnalysisRequest
            {
                AoiId = request.AoiId,
                PolygonGeoJson = request.PolygonGeoJson,
                StartYear = request.StartYear,
                EndYear = request.EndYear
            };

            try
            {
                var summary = pipeline.Run(analysisRequest);
                await TryPersistAsync(repository, loggerFactory, summary, analysisRequest, cancellationToken);
                return Results.Ok(ToResponse(summary));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                return Results.BadRequest(new ApiError { Message = exception.Message });
            }
        });

        group.MapGet("/analyses/{aoiId}/summary", (
            string aoiId,
            int startYear,
            int endYear,
            IAnalysisPipeline pipeline) =>
            RunAnalysis(pipeline, new AnalysisRequest
            {
                AoiId = aoiId,
                StartYear = startYear,
                EndYear = endYear
            }));

        group.MapPost("/analyses/{aoiId}/reports", (
            string aoiId,
            int startYear,
            int endYear,
            IAnalysisPipeline pipeline,
            IReportService reportService) =>
            GenerateReport(pipeline, reportService, new AnalysisRequest
            {
                AoiId = aoiId,
                StartYear = startYear,
                EndYear = endYear
            }));

        group.MapGet("/sources", (IOptions<DataOptions> dataOptions) =>
            Results.Ok(ReadSourceIds(dataOptions.Value)));

        group.MapGet("/analyses/{aoiId}/changes", (
            string aoiId,
            int startYear,
            int endYear,
            IAnalysisPipeline pipeline) =>
        {
            try
            {
                var summary = pipeline.Run(new AnalysisRequest
                {
                    AoiId = aoiId,
                    StartYear = startYear,
                    EndYear = endYear
                });

                var writer = new NetTopologySuite.IO.GeoJsonWriter();
                var features = summary.ChangeZones.Select(zone => new
                {
                    type = "Feature",
                    geometry = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(
                        writer.Write(zone.Geometry)),
                    properties = new
                    {
                        id = zone.Id,
                        areaHectares = zone.AreaHectares,
                        contributionToDeltaCarbon = zone.ContributionToDeltaCarbon,
                        pixelCount = zone.PixelCount
                    }
                }).ToArray();

                return Results.Ok(new { type = "FeatureCollection", features });
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                return Results.BadRequest(new ApiError { Message = exception.Message });
            }
        });

        group.MapGet("/experiments/sensitivity", (
            string aoiId,
            int startYear,
            int endYear,
            IAnalysisPipeline pipeline,
            ISpectralIndexService spectralIndexService,
            ISentinelSceneSelector sceneSelector,
            IOptions<CalculationOptions> calculationOptions) =>
        {
            var k1 = pipeline.Run(new AnalysisRequest
            {
                AoiId = aoiId,
                StartYear = startYear,
                EndYear = endYear,
                SensitivityCoefficient = 1
            });
            var k2 = pipeline.Run(new AnalysisRequest
            {
                AoiId = aoiId,
                StartYear = startYear,
                EndYear = endYear,
                SensitivityCoefficient = 2
            });

            var scenePair = sceneSelector.SelectPair(aoiId, startYear, endYear);
            var strictValidPixels = 0;
            var extendedValidPixels = 0;

            if (scenePair is not null)
            {
                var options = calculationOptions.Value;
                strictValidPixels = spectralIndexService.Calculate(
                    aoiId,
                    scenePair.BeforeReflectanceFileName,
                    scenePair.BeforeSclFileName,
                    options.AllowedSclClasses).ValidPixelCount;
                extendedValidPixels = spectralIndexService.Calculate(
                    aoiId,
                    scenePair.BeforeReflectanceFileName,
                    scenePair.BeforeSclFileName,
                    options.AllowedSclClasses.Concat(options.ExtendedSclClasses).ToArray()).ValidPixelCount;
            }

            return Results.Ok(new
            {
                aoiId,
                startYear,
                endYear,
                k1 = new
                {
                    lower = k1.Uncertainty.Lower,
                    upper = k1.Uncertainty.Upper,
                    halfWidth = k1.Uncertainty.HalfWidth,
                    units = k1.Units.Units
                },
                k2 = new
                {
                    lower = k2.Uncertainty.Lower,
                    upper = k2.Uncertainty.Upper,
                    halfWidth = k2.Uncertainty.HalfWidth,
                    units = k2.Units.Units
                },
                sclStrictValidPixels = strictValidPixels,
                sclExtendedValidPixels = extendedValidPixels
            });
        });

        return endpoints;
    }

    private static IResult RunAnalysis(IAnalysisPipeline pipeline, AnalysisRequest request)
    {
        try
        {
            var summary = pipeline.Run(request);
            return Results.Ok(ToResponse(summary));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return Results.BadRequest(new ApiError { Message = exception.Message });
        }
    }

    private static IResult GenerateReport(
        IAnalysisPipeline pipeline,
        IReportService reportService,
        AnalysisRequest request)
    {
        try
        {
            var summary = pipeline.Run(request);
            var report = reportService.Generate(summary);
            return Results.File(report.Pdf, "application/pdf");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return Results.BadRequest(new ApiError { Message = exception.Message });
        }
    }

    private static async Task TryPersistAsync(
        IAnalysisRunRepository repository,
        ILoggerFactory loggerFactory,
        AnalysisSummary summary,
        AnalysisRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await repository.SaveAsync(summary, request, cancellationToken);
        }
        catch (Exception exception)
        {
            loggerFactory
                .CreateLogger("AnalysisEndpoints")
                .LogWarning(exception, "Не удалось сохранить запуск анализа в базу данных.");
        }
    }

    private static IReadOnlyList<string> ReadSourceIds(DataOptions options)
    {
        var path = Path.Join(options.DataRoot, SourcesFileName);
        if (!File.Exists(path))
            return Array.Empty<string>();

        var document = new CsvDocument(File.ReadAllText(path));
        return document.Rows
            .Select(row => document.GetString(row, "source_id"))
            .ToArray();
    }

    private static AnalysisSummaryResponse ToResponse(AnalysisSummary summary) => new()
    {
        RunId = summary.RunId,
        MethodVersion = summary.MethodVersion,
        DataVersion = summary.DataVersion,
        CreatedAt = summary.CreatedAt,
        Status = summary.Status.ToString(),
        AoiId = summary.AoiId,
        StartYear = summary.StartYear,
        EndYear = summary.EndYear,
        PolygonAreaHectares = summary.PolygonAreaHectares,
        YearlySeries = summary.YearlySeries
            .Select(item => new YearlySeriesItem
            {
                Year = item.Year,
                AreaHectares = item.AreaHectares,
                TotalCarbon = item.TotalCarbon,
                MeanCarbonPerHectare = item.MeanCarbonPerHectare,
                Coverage = item.Coverage
            })
            .ToArray(),
        Change = new ChangeResponse
        {
            DeltaCarbon = summary.Change.DeltaCarbon,
            ProjectEmission = summary.Change.ProjectEmission,
            EmissionPerHectarePerYear = summary.Change.EmissionPerHectarePerYear
        },
        Uncertainty = new UncertaintyResponse
        {
            Lower = summary.Uncertainty.Lower,
            Upper = summary.Uncertainty.Upper,
            HalfWidth = summary.Uncertainty.HalfWidth
        },
        Baseline = new BaselineResponse
        {
            BaselineEmission = summary.Baseline.BaselineEmission
        },
        Units = new UnitsResponse
        {
            Status = summary.Units.Status.ToString(),
            Reason = summary.Units.Reason.ToString(),
            ResultRelativeToBaseline = summary.Units.ResultRelativeToBaseline,
            UncertaintyDeduction = summary.Units.UncertaintyDeduction,
            AdjustedResult = summary.Units.AdjustedResult,
            Reserve = summary.Units.Reserve,
            Units = summary.Units.Units
        },
        ChangeZones = summary.ChangeZones
            .Select(zone => new ChangeZoneResponse
            {
                Id = zone.Id,
                AreaHectares = zone.AreaHectares,
                ContributionToDeltaCarbon = zone.ContributionToDeltaCarbon
            })
            .ToArray(),
        ChangeZoneEvidence = summary.ChangeZoneEvidence
            .Select(evidence => new ChangeZoneEvidenceResponse
            {
                ZoneId = evidence.ZoneId,
                EvidenceTypes = evidence.EvidenceTypes.Select(type => type.ToString()).ToArray(),
                CauseStatus = evidence.CauseStatus.ToString()
            })
            .ToArray(),
        CciChangeMeanTonnesPerHectare = summary.CciChangeMeanTonnesPerHectare,
        Warnings = summary.Warnings
    };
}
