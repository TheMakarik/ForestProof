using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Углеродные метрики запуска.
/// </summary>
public sealed class CarbonMetrics
{
    /// <summary>
    /// Идентификатор запуска.
    /// </summary>
    [Key]
    public Guid RunId { get; set; }

    /// <summary>
    /// Расчётная площадь, га (A).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double CalculatedAreaHa { get; set; }

    /// <summary>
    /// Запас углерода в начальном году, т C.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double CT0 { get; set; }

    /// <summary>
    /// Запас углерода в конечном году, т C.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double CT1 { get; set; }

    /// <summary>
    /// Изменение запаса, т C (ΔC).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double DeltaC { get; set; }

    /// <summary>
    /// Изменение в CO₂-эквиваленте, т CO₂-экв. (Eproj).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double EProj { get; set; }

    /// <summary>
    /// Изменение на гектар и год (e).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double EPerHaYear { get; set; }

    /// <summary>
    /// Нижняя граница диапазона (L).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double LBound { get; set; }

    /// <summary>
    /// Верхняя граница диапазона (U).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double UBound { get; set; }

    /// <summary>
    /// Ширина диапазона (H).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double HValue { get; set; }

    /// <summary>
    /// Запуск.
    /// </summary>
    [ForeignKey(nameof(RunId))]
    public AnalysisRun? Run { get; set; }
}
