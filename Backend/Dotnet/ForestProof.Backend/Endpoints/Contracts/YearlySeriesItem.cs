namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Запас углерода и покрытие за один год периода.
/// </summary>
public sealed record YearlySeriesItem
{
    /// <summary>
    /// Год.
    /// </summary>
    public required int Year { get; init; }

    /// <summary>
    /// Расчётная площадь по валидным пикселям, га.
    /// </summary>
    public required double AreaHectares { get; init; }

    /// <summary>
    /// Суммарный запас углерода, т C.
    /// </summary>
    public required double TotalCarbon { get; init; }

    /// <summary>
    /// Средний запас углерода на гектар, т C/га.
    /// </summary>
    public required double MeanCarbonPerHectare { get; init; }

    /// <summary>
    /// Доля валидной площади от площади полигона.
    /// </summary>
    public required double Coverage { get; init; }
}
