using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Наблюдение.
/// </summary>
public sealed class Observation
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор файлового актива.
    /// </summary>
    public Guid SourceAssetId { get; set; }

    /// <summary>
    /// Идентификатор AOI.
    /// </summary>
    [MaxLength(64)]
    public string? AoiId { get; set; }

    /// <summary>
    /// Дата наблюдения.
    /// </summary>
    public DateOnly? ObservedDate { get; set; }

    /// <summary>
    /// Доля облачности.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double? CloudFraction { get; set; }

    /// <summary>
    /// Доля валидных данных.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double? ValidFraction { get; set; }

    /// <summary>
    /// Файловый актив.
    /// </summary>
    [ForeignKey(nameof(SourceAssetId))]
    public SourceAsset? SourceAsset { get; set; }

    /// <summary>
    /// Территория.
    /// </summary>
    [ForeignKey(nameof(AoiId))]
    public Area? Area { get; set; }
}
