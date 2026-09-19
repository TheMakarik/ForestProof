using ForestProof.Backend.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForestProof.Backend.Persistence;

/// <summary>
/// Контекст базы данных ForestProof (PostgreSQL/PostGIS).
/// </summary>
/// <param name="options">Параметры контекста.</param>
public sealed class ForestProofDbContext(DbContextOptions<ForestProofDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Организации.
    /// </summary>
    public DbSet<Organization> Organizations => Set<Organization>();

    /// <summary>
    /// Пользователи.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Источники данных.
    /// </summary>
    public DbSet<Source> Sources => Set<Source>();

    /// <summary>
    /// Методические параметры.
    /// </summary>
    public DbSet<Parameter> Parameters => Set<Parameter>();

    /// <summary>
    /// Территории.
    /// </summary>
    public DbSet<Area> Areas => Set<Area>();

    /// <summary>
    /// Базовые линии.
    /// </summary>
    public DbSet<Baseline> Baselines => Set<Baseline>();

    /// <summary>
    /// Файловые активы.
    /// </summary>
    public DbSet<SourceAsset> SourceAssets => Set<SourceAsset>();

    /// <summary>
    /// Наблюдения.
    /// </summary>
    public DbSet<Observation> Observations => Set<Observation>();

    /// <summary>
    /// Проекты.
    /// </summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>
    /// Запуски анализа.
    /// </summary>
    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();

    /// <summary>
    /// Углеродные метрики.
    /// </summary>
    public DbSet<CarbonMetrics> CarbonMetrics => Set<CarbonMetrics>();

    /// <summary>
    /// Годовой ряд.
    /// </summary>
    public DbSet<YearlySeries> YearlySeries => Set<YearlySeries>();

    /// <summary>
    /// Результаты базовой линии.
    /// </summary>
    public DbSet<BaselineResultEntity> BaselineResults => Set<BaselineResultEntity>();

    /// <summary>
    /// Сценарные стоимости.
    /// </summary>
    public DbSet<ScenarioValuation> ScenarioValuations => Set<ScenarioValuation>();

    /// <summary>
    /// Зоны изменений.
    /// </summary>
    public DbSet<ChangeZoneEntity> ChangeZones => Set<ChangeZoneEntity>();

    /// <summary>
    /// Подтверждения зон.
    /// </summary>
    public DbSet<ZoneEvidence> ZoneEvidence => Set<ZoneEvidence>();

    /// <summary>
    /// Отчёты.
    /// </summary>
    public DbSet<ReportEntity> Reports => Set<ReportEntity>();

    /// <summary>
    /// Риски.
    /// </summary>
    public DbSet<Risk> Risks => Set<Risk>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<YearlySeries>().HasKey(series => new { series.RunId, series.Year });
        modelBuilder.Entity<ScenarioValuation>().HasKey(valuation => new { valuation.RunId, valuation.PriceRub });

        modelBuilder.Entity<AnalysisRun>().Property(run => run.Status).HasConversion<string>();
        modelBuilder.Entity<User>().Property(user => user.Role).HasConversion<string>();
        modelBuilder.Entity<BaselineResultEntity>().Property(result => result.QStatus).HasConversion<string>();
        modelBuilder.Entity<ChangeZoneEntity>().Property(zone => zone.CauseStatus).HasConversion<string>();
        modelBuilder.Entity<ZoneEvidence>().Property(evidence => evidence.EvidenceType).HasConversion<string>();
        modelBuilder.Entity<ReportEntity>().Property(report => report.Format).HasConversion<string>();
    }
}
