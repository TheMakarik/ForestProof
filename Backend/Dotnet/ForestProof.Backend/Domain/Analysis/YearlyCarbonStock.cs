namespace ForestProof.Backend.Domain.Analysis;

/// <summary>
/// Запас углерода и покрытие за один год периода.
/// </summary>
public sealed record YearlyCarbonStock
{
    /// <summary>
    /// Год.
    /// </summary>
    public required int Year { get; init; }

    /// <summary>
    /// Расчётная площадь по валидным пикселям, га (A).
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

    /// <summary>
    /// Доля валидной площади от площади полигона (coverage).
    /// </summary>
    public required double Coverage { get; init; }

    /// <summary>
    /// Признак полного обязательного покрытия.
    /// </summary>
    public required bool HasCompleteCoverage { get; init; }
}
