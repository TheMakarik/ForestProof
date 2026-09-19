using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Domain.Enums;
using ForestProof.Backend.Persistence.Entities;
using ForestProof.Backend.Persistence.Enums;
using ForestProof.Backend.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ForestProof.Backend.Persistence;

/// <summary>
/// Сохраняет результаты расчёта в PostgreSQL/PostGIS.
/// </summary>
/// <param name="contextFactory">Фабрика контекста базы данных.</param>
public sealed class AnalysisRunRepository(IDbContextFactory<ForestProofDbContext> contextFactory) : IAnalysisRunRepository
{
    /// <inheritdoc />
    public async Task<Guid> SaveAsync(
        AnalysisSummary summary,
        AnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        var runId = Guid.TryParse(summary.RunId, out var parsed) ? parsed : Guid.NewGuid();
        var run = BuildRun(runId, summary, request);

        dbContext.AnalysisRuns.Add(run);
        await dbContext.SaveChangesAsync(cancellationToken);

        return runId;
    }

    private static AnalysisRun BuildRun(Guid runId, AnalysisSummary summary, AnalysisRequest request)
    {
        var firstYear = summary.YearlySeries[0];
        var lastYear = summary.YearlySeries[^1];

        var run = new AnalysisRun
        {
            Id = runId,
            AoiId = string.IsNullOrWhiteSpace(request.AoiId) ? null : request.AoiId,
            YearStart = summary.StartYear,
            YearEnd = summary.EndYear,
            KSensitivity = request.SensitivityCoefficient ?? 0,
            Status = MapStatus(summary.Status),
            CreatedAt = summary.CreatedAt,
            MethodVersion = summary.MethodVersion,
            DataVersion = summary.DataVersion,
            CarbonMetrics = new CarbonMetrics
            {
                RunId = runId,
                CalculatedAreaHa = summary.PolygonAreaHectares,
                CT0 = firstYear.TotalCarbon,
                CT1 = lastYear.TotalCarbon,
                DeltaC = summary.Change.DeltaCarbon,
                EProj = summary.Change.ProjectEmission,
                EPerHaYear = summary.Change.EmissionPerHectarePerYear,
                LBound = summary.Uncertainty.Lower,
                UBound = summary.Uncertainty.Upper,
                HValue = summary.Uncertainty.HalfWidth
            },
            BaselineResult = new BaselineResultEntity
            {
                RunId = runId,
                EBase = summary.Baseline.BaselineEmission,
                RValue = summary.Units.ResultRelativeToBaseline,
                Unc = summary.Units.UncertaintyDeduction,
                RAdj = summary.Units.AdjustedResult,
                ReserveB = summary.Units.Reserve,
                QUnits = summary.Units.Units,
                QStatus = MapQStatus(summary.Units.Status),
                BlockReason = summary.Units.Reason.ToString()
            },
            YearlySeries = summary.YearlySeries
                .Select(item => new YearlySeries
                {
                    RunId = runId,
                    Year = item.Year,
                    CBarT = item.MeanCarbonPerHectare,
                    CT = item.TotalCarbon
                })
                .ToList(),
            ScenarioValuations = summary.Units.PriceScenarios
                .Select(scenario => new ScenarioValuation
                {
                    RunId = runId,
                    PriceRub = scenario.PricePerUnit,
                    TotalValueRub = scenario.Value
                })
                .ToList(),
            Risks = summary.Warnings
                .Select(message => new Risk
                {
                    Id = Guid.NewGuid(),
                    RunId = runId,
                    RiskType = "warning",
                    Message = message
                })
                .ToList()
        };

        var evidenceByZone = summary.ChangeZoneEvidence.ToDictionary(evidence => evidence.ZoneId);

        foreach (var zone in summary.ChangeZones)
        {
            var zoneId = Guid.NewGuid();
            var zoneEntity = new ChangeZoneEntity
            {
                Id = zoneId,
                RunId = runId,
                Geometry = zone.Geometry,
                AreaHa = zone.AreaHectares,
                ContributionTc = zone.ContributionToDeltaCarbon,
                CauseStatus = evidenceByZone.TryGetValue(zone.Id, out var evidence)
                    ? MapCauseStatus(evidence.CauseStatus)
                    : ZoneCauseStatus.Undetermined
            };

            if (evidenceByZone.TryGetValue(zone.Id, out var zoneEvidence))
            {
                foreach (var type in zoneEvidence.EvidenceTypes)
                {
                    zoneEntity.ZoneEvidence.Add(new ZoneEvidence
                    {
                        Id = Guid.NewGuid(),
                        ZoneId = zoneId,
                        EvidenceType = MapEvidenceType(type)
                    });
                }
            }

            run.ChangeZones.Add(zoneEntity);
        }

        return run;
    }

    private static AnalysisStatus MapStatus(RunStatus status) => status switch
    {
        RunStatus.Complete => AnalysisStatus.Complete,
        RunStatus.Partial => AnalysisStatus.Partial,
        RunStatus.UnitsUnavailable => AnalysisStatus.UnitsUnavailable,
        _ => AnalysisStatus.Failed
    };

    private static QStatus MapQStatus(UnitStatus status) => status switch
    {
        UnitStatus.Available => QStatus.Computed,
        UnitStatus.Zero => QStatus.Zero,
        _ => QStatus.Unavailable
    };

    private static ZoneCauseStatus MapCauseStatus(CauseStatus status) => status switch
    {
        CauseStatus.Confirmed => ZoneCauseStatus.Confirmed,
        CauseStatus.Probable => ZoneCauseStatus.Likely,
        _ => ZoneCauseStatus.Undetermined
    };

    private static ZoneEvidenceType MapEvidenceType(EvidenceType type) => type switch
    {
        EvidenceType.Gfc => ZoneEvidenceType.Gfc,
        EvidenceType.Sentinel2 => ZoneEvidenceType.Sentinel2,
        EvidenceType.Modis => ZoneEvidenceType.Modis,
        _ => ZoneEvidenceType.CciChange
    };
}
