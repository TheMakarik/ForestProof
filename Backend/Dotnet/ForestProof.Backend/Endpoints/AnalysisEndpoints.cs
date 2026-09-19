using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Endpoints.Contracts;
using ForestProof.Backend.Options;
using ForestProof.Backend.Persistence.Entities;
using ForestProof.Backend.Persistence.Interfaces;
using ForestProof.Backend.Services.Analysis;
using ForestProof.Backend.Services.Analysis.Interfaces;
using ForestProof.Backend.Services.ChangeZones;
using ForestProof.Backend.Services.Data;
using ForestProof.Backend.Services.Raster.Interfaces;
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
            IAnalysisResultCache cache,
            IAnalysisRunRepository repository,
            IOptions<CalculationOptions> calculationOptions,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (!IsKnownMethodProfile(request.MethodProfile))
                return Results.BadRequest(new ApiError { Message = $"unknown methodProfile: {request.MethodProfile}" });

            var analysisRequest = ToAnalysisRequest(request);

            var options = calculationOptions.Value;
            var sensitivity = analysisRequest.SensitivityCoefficient ?? options.DefaultSensitivityCoefficient;
            var inputHash = AnalysisInputHash.Compute(analysisRequest, sensitivity);

            if (cache.TryGet(inputHash, options.MethodVersion, out var cached))
                return Results.Ok(ToResponse(cached));

            try
            {
                var summary = pipeline.Run(analysisRequest);
                cache.Store(summary);
                await TryPersistAsync(repository, loggerFactory, summary, analysisRequest, cancellationToken);
                return Results.Ok(ToResponse(summary));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                return Results.BadRequest(new ApiError { Message = exception.Message });
            }
        });

        group.MapPost("/analyses/changes", (
            CreateAnalysisRequest request,
            IAnalysisPipeline pipeline) => BuildChanges(pipeline, ToAnalysisRequest(request)));

        group.MapPost("/analyses/reports", async (
            CreateReportRequest request,
            IAnalysisPipeline pipeline,
            IReportService reportService,
            IAnalysisRunRepository repository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
            await BuildReport(
                pipeline,
                reportService,
                repository,
                loggerFactory,
                ToAnalysisRequest(request),
                request.Format,
                cancellationToken));

        group.MapGet("/runs/{runId}", async (
            string runId,
            IAnalysisRunRepository repository,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(runId, out var id))
                return Results.NotFound(new ApiError { Message = $"run not found: {runId}" });

            try
            {
                var run = await repository.GetAsync(id, cancellationToken);
                if (run is null)
                    return Results.NotFound(new ApiError { Message = $"run not found: {runId}" });

                return Results.Ok(ToRunResponse(run));
            }
            catch (Exception exception)
            {
                return Results.Json(
                    new ApiError { Message = "run lookup unavailable: " + exception.Message },
                    statusCode: StatusCodes.Status503ServiceUnavailable);
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

        group.MapPost("/analyses/{aoiId}/reports", async (
            string aoiId,
            int startYear,
            int endYear,
            string? format,
            IAnalysisPipeline pipeline,
            IReportService reportService,
            IAnalysisRunRepository repository,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
            await BuildReport(
                pipeline,
                reportService,
                repository,
                loggerFactory,
                new AnalysisRequest
                {
                    AoiId = aoiId,
                    StartYear = startYear,
                    EndYear = endYear
                },
                format,
                cancellationToken));

        group.MapGet("/analyses/{aoiId}/yearly.csv", (
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

                var builder = new System.Text.StringBuilder();
                builder.AppendLine("year,area_ha,total_carbon_tc,mean_carbon_tc_ha,coverage");
                foreach (var item in summary.YearlySeries)
                {
                    builder.AppendLine(string.Join(
                        ',',
                        item.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        item.AreaHectares.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        item.TotalCarbon.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        item.MeanCarbonPerHectare.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        item.Coverage.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }

                return Results.File(
                    System.Text.Encoding.UTF8.GetBytes(builder.ToString()),
                    "text/csv",
                    $"yearly-{aoiId}.csv");
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                return Results.BadRequest(new ApiError { Message = exception.Message });
            }
        });

        group.MapGet("/sources", (IOptions<DataOptions> dataOptions) =>
            Results.Ok(ReadSources(dataOptions.Value)));

        group.MapGet("/analyses/{aoiId}/changes", (
            string aoiId,
            int startYear,
            int endYear,
            IAnalysisPipeline pipeline) =>
            BuildChanges(pipeline, new AnalysisRequest
            {
                AoiId = aoiId,
                StartYear = startYear,
                EndYear = endYear
            }));

        group.MapGet("/experiments/sensitivity", (
            string aoiId,
            int startYear,
            int endYear,
            IAnalysisPipeline pipeline,
            ISpectralIndexService spectralIndexService,
            ISentinelSceneSelector sceneSelector,
            IRasterService rasterService,
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
            var sclExtended = pipeline.Run(new AnalysisRequest
            {
                AoiId = aoiId,
                StartYear = startYear,
                EndYear = endYear,
                UseExtendedSclClasses = true
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

            var cciChangeMean = Mean(rasterService.ReadChange(aoiId).AgbDifference);

            var biomass2019 = rasterService.ReadBiomass(aoiId, 2019);
            var biomass2020 = rasterService.ReadBiomass(aoiId, 2020);
            var length = Math.Min(biomass2019.Biomass.Count, biomass2020.Biomass.Count);
            var selfDifferences = new List<double?>(length);
            for (var index = 0; index < length; index++)
            {
                var before = biomass2019.Biomass[index];
                var after = biomass2020.Biomass[index];
                selfDifferences.Add(before is not null && after is not null ? after.Value - before.Value : null);
            }

            var selfDifferenceMean = Mean(selfDifferences);

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
                    units = k1.Units.Units,
                    eproj = k1.Change.ProjectEmission,
                    coverage = k1.YearlySeries[0].Coverage,
                    zoneAreaHectares = k1.ChangeZones.Sum(zone => zone.AreaHectares)
                },
                k2 = new
                {
                    lower = k2.Uncertainty.Lower,
                    upper = k2.Uncertainty.Upper,
                    halfWidth = k2.Uncertainty.HalfWidth,
                    units = k2.Units.Units,
                    eproj = k2.Change.ProjectEmission,
                    coverage = k2.YearlySeries[0].Coverage,
                    zoneAreaHectares = k2.ChangeZones.Sum(zone => zone.AreaHectares)
                },
                sclStrict = new
                {
                    units = k1.Units.Units,
                    eproj = k1.Change.ProjectEmission,
                    coverage = k1.YearlySeries[0].Coverage,
                    zoneAreaHectares = k1.ChangeZones.Sum(zone => zone.AreaHectares)
                },
                sclExtended = new
                {
                    units = sclExtended.Units.Units,
                    eproj = sclExtended.Change.ProjectEmission,
                    coverage = sclExtended.YearlySeries[0].Coverage,
                    zoneAreaHectares = sclExtended.ChangeZones.Sum(zone => zone.AreaHectares)
                },
                sclStrictValidPixels = strictValidPixels,
                sclExtendedValidPixels = extendedValidPixels,
                cciChangeMean,
                selfDifferenceMean
            });
        });

        return endpoints;
    }

    private static double? Mean(IEnumerable<double?> values)
    {
        var sum = 0d;
        var count = 0;
        foreach (var value in values)
        {
            if (value is null)
                continue;

            sum += value.Value;
            count++;
        }

        return count == 0 ? null : sum / count;
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

    private static AnalysisRequest ToAnalysisRequest(CreateAnalysisRequest request)
    {
        var analysisRequest = new AnalysisRequest
        {
            AoiId = request.AoiId,
            PolygonGeoJson = request.PolygonGeoJson,
            StartYear = request.StartYear,
            EndYear = request.EndYear
        };

        return request.MethodProfile switch
        {
            "extended_scl" => analysisRequest with { UseExtendedSclClasses = true },
            "k2" => analysisRequest with { SensitivityCoefficient = 2 },
            _ => analysisRequest
        };
    }

    private static AnalysisRequest ToAnalysisRequest(CreateReportRequest request) => new()
    {
        AoiId = request.AoiId,
        PolygonGeoJson = request.PolygonGeoJson,
        StartYear = request.StartYear,
        EndYear = request.EndYear
    };

    private static bool IsKnownMethodProfile(string? methodProfile) =>
        string.IsNullOrEmpty(methodProfile) || methodProfile is "default" or "extended_scl" or "k2";

    private static IResult BuildChanges(IAnalysisPipeline pipeline, AnalysisRequest request)
    {
        try
        {
            var summary = pipeline.Run(request);

            var evidenceByZone = summary.ChangeZoneEvidence.ToDictionary(evidence => evidence.ZoneId);
            var writer = new NetTopologySuite.IO.GeoJsonWriter();
            var features = new List<object>();

            foreach (var zone in summary.ChangeZones)
            {
                evidenceByZone.TryGetValue(zone.Id, out var evidence);

                features.Add(new
                {
                    type = "Feature",
                    geometry = zone.Geometry is { } geometry
                        ? System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(writer.Write(geometry))
                        : (System.Text.Json.JsonElement?)null,
                    properties = new
                    {
                        id = zone.Id,
                        areaHectares = zone.AreaHectares,
                        contributionToDeltaCarbon = zone.ContributionToDeltaCarbon,
                        pixelCount = zone.PixelCount,
                        evidenceTypes = evidence?.EvidenceTypes.Select(type => type.ToString()).ToArray()
                            ?? Array.Empty<string>(),
                        causeStatus = evidence?.CauseStatus.ToString() ?? "Unknown",
                        interpretation = evidence?.Interpretation
                            ?? CauseStatusRules.Interpretation(CauseStatus.Unknown)
                    }
                });
            }

            return Results.Ok(new { type = "FeatureCollection", features });
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return Results.BadRequest(new ApiError { Message = exception.Message });
        }
    }

    private static async Task<IResult> BuildReport(
        IAnalysisPipeline pipeline,
        IReportService reportService,
        IAnalysisRunRepository repository,
        ILoggerFactory loggerFactory,
        AnalysisRequest request,
        string? format,
        CancellationToken cancellationToken)
    {
        try
        {
            var summary = pipeline.Run(request);
            var report = reportService.Generate(summary);

            await TrySaveReportAsync(repository, loggerFactory, summary.RunId, format ?? "pdf", cancellationToken);

            return format switch
            {
                "html" => Results.Text(report.Html, "text/html; charset=utf-8"),
                "json" => Results.Text(report.Json, "application/json; charset=utf-8"),
                _ => Results.File(report.Pdf, "application/pdf")
            };
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            return Results.BadRequest(new ApiError { Message = exception.Message });
        }
    }

    private static async Task TrySaveReportAsync(
        IAnalysisRunRepository repository,
        ILoggerFactory loggerFactory,
        string runId,
        string format,
        CancellationToken cancellationToken)
    {
        try
        {
            await repository.SaveReportAsync(Guid.Parse(runId), format, null, cancellationToken);
        }
        catch (Exception exception)
        {
            loggerFactory
                .CreateLogger("AnalysisEndpoints")
                .LogWarning(exception, "Не удалось сохранить отчёт в базу данных.");
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

    private static IReadOnlyList<object> ReadSources(DataOptions options)
    {
        var path = Path.Join(options.DataRoot, SourcesFileName);
        if (!File.Exists(path))
            return Array.Empty<object>();

        var document = new CsvDocument(File.ReadAllText(path));
        return document.Rows
            .Select(row => (object)new
            {
                sourceId = document.GetString(row, "source_id"),
                product = document.GetString(row, "product"),
                version = document.GetString(row, "version"),
                licenseUrl = document.GetString(row, "license_url")
            })
            .ToArray();
    }

    private static AnalysisSummaryResponse ToResponse(AnalysisSummary summary) => new()
    {
        RunId = summary.RunId,
        MethodVersion = summary.MethodVersion,
        DataVersion = summary.DataVersion,
        CreatedAt = summary.CreatedAt,
        InputHash = summary.InputHash,
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
            Units = summary.Units.Units,
            PriceScenarios = summary.Units.PriceScenarios
                .Select(scenario => new PriceScenarioResponse
                {
                    PricePerUnit = scenario.PricePerUnit,
                    Value = scenario.Value
                })
                .ToArray()
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

    private static AnalysisRunResponse ToRunResponse(AnalysisRun run) => new()
    {
        RunId = run.Id.ToString("N"),
        Status = run.Status.ToString(),
        MethodVersion = run.MethodVersion ?? string.Empty,
        DataVersion = run.DataVersion ?? string.Empty,
        CreatedAt = run.CreatedAt,
        InputHash = run.InputHash ?? string.Empty,
        AoiId = run.AoiId,
        StartYear = run.YearStart,
        YearEnd = run.YearEnd,
        PolygonAreaHectares = run.CarbonMetrics?.CalculatedAreaHa ?? 0,
        Units = run.BaselineResult?.QUnits,
        QStatus = run.BaselineResult?.QStatus.ToString(),
        Warnings = run.Risks
            .Select(risk => risk.Message ?? string.Empty)
            .ToArray()
    };
}
