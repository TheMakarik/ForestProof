namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Сценарная стоимость потенциальных единиц при заданной цене.
/// </summary>
public sealed record PriceScenarioResponse
{
    /// <summary>
    /// Сценарная цена одной единицы, руб.
    /// </summary>
    public required double PricePerUnit { get; init; }

    /// <summary>
    /// Сценарная стоимость, руб.
    /// </summary>
    public required double Value { get; init; }
}
