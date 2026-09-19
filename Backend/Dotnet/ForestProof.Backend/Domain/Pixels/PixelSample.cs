namespace ForestProof.Backend.Domain.Pixels;

/// <summary>
/// Пиксель растра с биомассой, площадью пересечения с полигоном и неопределённостью.
/// </summary>
public sealed record PixelSample
{
    /// <summary>
    /// Надземная древесная биомасса, т/га (AGB).
    /// </summary>
    public required double Biomass { get; init; }

    /// <summary>
    /// Площадь пересечения пикселя с полигоном, га (aᵢ).
    /// </summary>
    public required double AreaHectares { get; init; }

    /// <summary>
    /// Стандартное отклонение оценки биомассы, т/га (SD); null — значение отсутствует.
    /// </summary>
    public double? StandardDeviation { get; init; }
}
