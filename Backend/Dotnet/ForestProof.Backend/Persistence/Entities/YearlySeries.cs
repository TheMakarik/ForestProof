using System.ComponentModel.DataAnnotations.Schema;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Точка годового ряда запаса.
/// </summary>
public sealed class YearlySeries
{
    /// <summary>
    /// Идентификатор запуска.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// Год.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Средний запас, т C/га (c̄ₜ).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double CBarT { get; set; }

    /// <summary>
    /// Суммарный запас, т C (Cₜ).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double CT { get; set; }

    /// <summary>
    /// Запуск.
    /// </summary>
    [ForeignKey(nameof(RunId))]
    public AnalysisRun? Run { get; set; }
}
