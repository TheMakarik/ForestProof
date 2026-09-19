using ForestProof.Backend.Domain.Enums;
using ForestProof.Backend.Domain.Pricing;

namespace ForestProof.Backend.Domain.Units;

/// <summary>
/// Результат расчёта потенциальных единиц с разделением доступного, нулевого и недоступного исхода.
/// </summary>
public sealed record UnitResult
{
    /// <summary>
    /// Статус расчёта единиц.
    /// </summary>
    public required UnitStatus Status { get; init; }

    /// <summary>
    /// Причина нулевого или недоступного результата.
    /// </summary>
    public required UnitBlockReason Reason { get; init; }

    /// <summary>
    /// Результат проекта относительно baseline, т CO₂-экв. (R).
    /// </summary>
    public required double ResultRelativeToBaseline { get; init; }

    /// <summary>
    /// Доля вычета за неопределённость (UNC).
    /// </summary>
    public required double UncertaintyDeduction { get; init; }

    /// <summary>
    /// Результат после вычета неопределённости, т CO₂-экв. (Radj).
    /// </summary>
    public required double AdjustedResult { get; init; }

    /// <summary>
    /// Резерв, т CO₂-экв. (B).
    /// </summary>
    public required double Reserve { get; init; }

    /// <summary>
    /// Потенциальные единицы (Q); null — расчёт недоступен.
    /// </summary>
    public required int? Units { get; init; }

    /// <summary>
    /// Сценарная стоимость единиц по заданным ценам.
    /// </summary>
    public required IReadOnlyList<PriceScenario> PriceScenarios { get; init; }
}
