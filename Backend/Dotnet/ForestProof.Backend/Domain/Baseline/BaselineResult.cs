namespace ForestProof.Backend.Domain.Baseline;

/// <summary>
/// Базовая линия: исторический темп, сценарные запасы на границах периода и Ebase.
/// </summary>
public sealed record BaselineResult
{
    /// <summary>
    /// Исторический годовой темп изменения удельного запаса, т C/га/год (g).
    /// </summary>
    public required double HistoricalRate { get; init; }

    /// <summary>
    /// Сценарный удельный запас в начальном году, т C/га (cbase,t₀).
    /// </summary>
    public required double StartCarbonPerHectare { get; init; }

    /// <summary>
    /// Сценарный удельный запас в конечном году, т C/га (cbase,t₁).
    /// </summary>
    public required double EndCarbonPerHectare { get; init; }

    /// <summary>
    /// Изменение запаса по базовой линии в CO₂-эквиваленте, т CO₂-экв. (Ebase).
    /// </summary>
    public required double BaselineEmission { get; init; }
}
