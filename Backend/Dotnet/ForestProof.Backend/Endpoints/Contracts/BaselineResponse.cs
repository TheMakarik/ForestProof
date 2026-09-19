namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Результат базовой линии.
/// </summary>
public sealed record BaselineResponse
{
    /// <summary>
    /// Изменение запаса по базовой линии в CO₂-эквиваленте, т CO₂-экв. (Ebase).
    /// </summary>
    public required double BaselineEmission { get; init; }
}
