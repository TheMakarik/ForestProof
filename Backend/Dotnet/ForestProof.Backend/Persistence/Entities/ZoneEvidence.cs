using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForestProof.Backend.Persistence.Enums;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Подтверждение зоны изменений.
/// </summary>
public sealed class ZoneEvidence
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор зоны.
    /// </summary>
    public Guid ZoneId { get; set; }

    /// <summary>
    /// Идентификатор файлового актива.
    /// </summary>
    public Guid? SourceAssetId { get; set; }

    /// <summary>
    /// Тип подтверждения.
    /// </summary>
    public ZoneEvidenceType EvidenceType { get; set; }

    /// <summary>
    /// Зона.
    /// </summary>
    [ForeignKey(nameof(ZoneId))]
    public ChangeZoneEntity? Zone { get; set; }

    /// <summary>
    /// Файловый актив.
    /// </summary>
    [ForeignKey(nameof(SourceAssetId))]
    public SourceAsset? SourceAsset { get; set; }
}
