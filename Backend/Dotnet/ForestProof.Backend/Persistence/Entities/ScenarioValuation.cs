using System.ComponentModel.DataAnnotations.Schema;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Сценарная стоимость единиц.
/// </summary>
public sealed class ScenarioValuation
{
    /// <summary>
    /// Идентификатор запуска.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// Сценарная цена единицы, руб.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double PriceRub { get; set; }

    /// <summary>
    /// Сценарная стоимость, руб.
    /// </summary>
    [Column(TypeName = "numeric")]
    public double TotalValueRub { get; set; }

    /// <summary>
    /// Запуск.
    /// </summary>
    [ForeignKey(nameof(RunId))]
    public AnalysisRun? Run { get; set; }
}
