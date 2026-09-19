using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForestProof.Backend.Persistence.Enums;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Результат сравнения с базовой линией и расчёта единиц.
/// </summary>
public sealed class BaselineResultEntity
{
    /// <summary>
    /// Идентификатор запуска.
    /// </summary>
    [Key]
    public Guid RunId { get; set; }

    /// <summary>
    /// Изменение по базовой линии, т CO₂-экв. (Ebase).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double EBase { get; set; }

    /// <summary>
    /// Результат относительно baseline, т CO₂-экв. (R).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double RValue { get; set; }

    /// <summary>
    /// Доля вычета за неопределённость (UNC).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double Unc { get; set; }

    /// <summary>
    /// Результат после вычета неопределённости (Radj).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double RAdj { get; set; }

    /// <summary>
    /// Резерв, т CO₂-экв. (B).
    /// </summary>
    [Column(TypeName = "numeric")]
    public double ReserveB { get; set; }

    /// <summary>
    /// Потенциальные единицы (Q).
    /// </summary>
    public int? QUnits { get; set; }

    /// <summary>
    /// Статус единиц.
    /// </summary>
    public QStatus QStatus { get; set; }

    /// <summary>
    /// Причина блокировки расчёта.
    /// </summary>
    [MaxLength(256)]
    public string? BlockReason { get; set; }

    /// <summary>
    /// Запуск.
    /// </summary>
    [ForeignKey(nameof(RunId))]
    public AnalysisRun? Run { get; set; }
}
