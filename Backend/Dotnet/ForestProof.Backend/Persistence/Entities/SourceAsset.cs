using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Файловый актив источника.
/// </summary>
public sealed class SourceAsset
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор источника.
    /// </summary>
    [MaxLength(64)]
    public string? SourceId { get; set; }

    /// <summary>
    /// Идентификатор AOI.
    /// </summary>
    [MaxLength(64)]
    public string? AoiId { get; set; }

    /// <summary>
    /// SHA-256 файла.
    /// </summary>
    [Column(TypeName = "char(64)")]
    public string? Sha256 { get; set; }

    /// <summary>
    /// Система координат.
    /// </summary>
    [MaxLength(64)]
    public string? Crs { get; set; }

    /// <summary>
    /// Источник.
    /// </summary>
    [ForeignKey(nameof(SourceId))]
    public Source? Source { get; set; }

    /// <summary>
    /// Территория.
    /// </summary>
    [ForeignKey(nameof(AoiId))]
    public Area? Area { get; set; }

    /// <summary>
    /// Наблюдения актива.
    /// </summary>
    public ICollection<Observation> Observations { get; set; } = [];

    /// <summary>
    /// Подтверждения зон, ссылающиеся на актив.
    /// </summary>
    public ICollection<ZoneEvidence> ZoneEvidence { get; set; } = [];
}
