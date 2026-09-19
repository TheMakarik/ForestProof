namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Результат расчёта потенциальных единиц.
/// </summary>
public sealed record UnitsResponse
{
    /// <summary>
    /// Статус расчёта единиц.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Причина нулевого или недоступного результата.
    /// </summary>
    public required string Reason { get; init; }

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
    public required IReadOnlyList<PriceScenarioResponse> PriceScenarios { get; init; }
}
