namespace ForestProof.Backend.Domain.Carbon;

/// <summary>
/// Суммарный запас углерода на территории за один год.
/// </summary>
public sealed record CarbonStock
{
    /// <summary>
    /// Расчётная площадь, га (A).
    /// </summary>
    public required double AreaHectares { get; init; }

    /// <summary>
    /// Суммарный запас углерода, т C (Cₜ).
    /// </summary>
    public required double TotalCarbon { get; init; }

    /// <summary>
    /// Средний запас углерода на гектар, т C/га (c̄ₜ).
    /// </summary>
    public required double MeanCarbonPerHectare { get; init; }
}
