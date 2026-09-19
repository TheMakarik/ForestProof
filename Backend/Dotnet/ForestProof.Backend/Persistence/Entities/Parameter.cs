using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Методический параметр.
/// </summary>
public sealed class Parameter
{
    /// <summary>
    /// Имя параметра.
    /// </summary>
    [Key]
    [Column("parameter")]
    [MaxLength(64)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Числовое значение.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double? ValueNumeric { get; set; }

    /// <summary>
    /// Идентификатор источника.
    /// </summary>
    [MaxLength(64)]
    public string? SourceId { get; set; }

    /// <summary>
    /// Источник.
    /// </summary>
    [ForeignKey(nameof(SourceId))]
    public Source? Source { get; set; }
}
