namespace ForestProof.Backend.Domain.Pricing;

/// <summary>
/// Сценарная стоимость потенциальных единиц при заданной цене.
/// </summary>
public sealed record PriceScenario
{
    /// <summary>
    /// Сценарная цена одной единицы, руб.
    /// </summary>
    public required double PricePerUnit { get; init; }

    /// <summary>
    /// Сценарная стоимость, руб. (V = Q × p).
    /// </summary>
    public required double Value { get; init; }
}
