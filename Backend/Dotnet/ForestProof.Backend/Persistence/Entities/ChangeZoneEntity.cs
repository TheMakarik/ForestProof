using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForestProof.Backend.Persistence.Enums;
using NetTopologySuite.Geometries;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Зона изменений.
/// </summary>
public sealed class ChangeZoneEntity
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор запуска.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// Геометрия зоны в WGS 84.
    /// </summary>
    [Column(TypeName = "geometry")]
    public Geometry? Geometry { get; set; }

    /// <summary>
    /// Площадь зоны, га.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double AreaHa { get; set; }

    /// <summary>
    /// Вклад в изменение запаса, т C.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double ContributionTc { get; set; }

    /// <summary>
    /// Статус причины.
    /// </summary>
    public ZoneCauseStatus CauseStatus { get; set; }

    /// <summary>
    /// Запуск.
    /// </summary>
    [ForeignKey(nameof(RunId))]
    public AnalysisRun? Run { get; set; }

    /// <summary>
    /// Подтверждения зоны.
    /// </summary>
    public ICollection<ZoneEvidence> ZoneEvidence { get; set; } = [];
}
