using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Базовая линия AOI.
/// </summary>
public sealed class Baseline
{
    /// <summary>
    /// Идентификатор AOI.
    /// </summary>
    [Key]
    [MaxLength(64)]
    public string AoiId { get; set; } = null!;

    /// <summary>
    /// Сценарный удельный запас 2015 года, т C/га.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double ReferenceMean2015TcHa { get; set; }

    /// <summary>
    /// Сценарный удельный запас 2019 года, т C/га.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double ReferenceMean2019TcHa { get; set; }

    /// <summary>
    /// Исторический годовой темп, т C/га/год.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double HistoricalRateTcHaYr { get; set; }

    /// <summary>
    /// Территория.
    /// </summary>
    [ForeignKey(nameof(AoiId))]
    public Area? Area { get; set; }
}
