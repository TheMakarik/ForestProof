using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForestProof.Backend.Persistence.Enums;
using NetTopologySuite.Geometries;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Запуск анализа.
/// </summary>
public sealed class AnalysisRun
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор проекта.
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Идентификатор AOI.
    /// </summary>
    [MaxLength(64)]
    public string? AoiId { get; set; }

    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Запрошенная геометрия (дочерний контур).
    /// </summary>
    [Column(TypeName = "geometry")]
    public Geometry? RequestedGeometry { get; set; }

    /// <summary>
    /// Начальный год.
    /// </summary>
    public int YearStart { get; set; }

    /// <summary>
    /// Конечный год.
    /// </summary>
    public int YearEnd { get; set; }

    /// <summary>
    /// Коэффициент чувствительности k.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double KSensitivity { get; set; }

    /// <summary>
    /// Режим SCL-маски.
    /// </summary>
    [MaxLength(32)]
    public string? SclMode { get; set; }

    /// <summary>
    /// Хэш входных параметров.
    /// </summary>
    [Column(TypeName = "char(64)")]
    public string? InputHash { get; set; }

    /// <summary>
    /// Статус.
    /// </summary>
    public AnalysisStatus Status { get; set; }

    /// <summary>
    /// Момент создания.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Версия методики.
    /// </summary>
    [MaxLength(32)]
    public string? MethodVersion { get; set; }

    /// <summary>
    /// Версия данных.
    /// </summary>
    [MaxLength(64)]
    public string? DataVersion { get; set; }

    /// <summary>
    /// Проект.
    /// </summary>
    [ForeignKey(nameof(ProjectId))]
    public Project? Project { get; set; }

    /// <summary>
    /// Территория.
    /// </summary>
    [ForeignKey(nameof(AoiId))]
    public Area? Area { get; set; }

    /// <summary>
    /// Пользователь.
    /// </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    /// <summary>
    /// Углеродные метрики.
    /// </summary>
    public CarbonMetrics? CarbonMetrics { get; set; }

    /// <summary>
    /// Результат базовой линии.
    /// </summary>
    public BaselineResultEntity? BaselineResult { get; set; }

    /// <summary>
    /// Годовой ряд.
    /// </summary>
    public ICollection<YearlySeries> YearlySeries { get; set; } = [];

    /// <summary>
    /// Сценарные стоимости.
    /// </summary>
    public ICollection<ScenarioValuation> ScenarioValuations { get; set; } = [];

    /// <summary>
    /// Зоны изменений.
    /// </summary>
    public ICollection<ChangeZoneEntity> ChangeZones { get; set; } = [];

    /// <summary>
    /// Отчёты.
    /// </summary>
    public ICollection<ReportEntity> Reports { get; set; } = [];

    /// <summary>
    /// Риски.
    /// </summary>
    public ICollection<Risk> Risks { get; set; } = [];
}
