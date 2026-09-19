using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Территория анализа.
/// </summary>
public sealed class Area
{
    /// <summary>
    /// Идентификатор AOI.
    /// </summary>
    [Key]
    [MaxLength(64)]
    public string AoiId { get; set; } = null!;

    /// <summary>
    /// Идентификатор родительского AOI.
    /// </summary>
    [MaxLength(64)]
    public string? ParentAoiId { get; set; }

    /// <summary>
    /// Геометрия в WGS 84.
    /// </summary>
    [Column(TypeName = "geometry (multipolygon)")]
    public MultiPolygon? Geometry { get; set; }

    /// <summary>
    /// Площадь, га.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double AreaHectares { get; set; }

    /// <summary>
    /// Роль участка в наборе.
    /// </summary>
    [MaxLength(256)]
    public string? SelectionRole { get; set; }

    /// <summary>
    /// Родительский AOI.
    /// </summary>
    [ForeignKey(nameof(ParentAoiId))]
    public Area? Parent { get; set; }

    /// <summary>
    /// Дочерние AOI.
    /// </summary>
    public ICollection<Area> Children { get; set; } = [];

    /// <summary>
    /// Базовая линия AOI.
    /// </summary>
    public Baseline? Baseline { get; set; }

    /// <summary>
    /// Проекты на территории.
    /// </summary>
    public ICollection<Project> Projects { get; set; } = [];

    /// <summary>
    /// Запуски анализа.
    /// </summary>
    public ICollection<AnalysisRun> AnalysisRuns { get; set; } = [];

    /// <summary>
    /// Файловые активы, покрывающие территорию.
    /// </summary>
    public ICollection<SourceAsset> SourceAssets { get; set; } = [];

    /// <summary>
    /// Наблюдения на территории.
    /// </summary>
    public ICollection<Observation> Observations { get; set; } = [];
}
