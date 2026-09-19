namespace ForestProof.Backend.Domain.Geometry;

/// <summary>
/// Площадь пересечения пикселя растра с полигоном.
/// </summary>
public sealed record PixelIntersection
{
    /// <summary>
    /// Строка пикселя в растровой сетке.
    /// </summary>
    public required int Row { get; init; }

    /// <summary>
    /// Столбец пикселя в растровой сетке.
    /// </summary>
    public required int Column { get; init; }

    /// <summary>
    /// Площадь пересечения пикселя с полигоном, га (aᵢ).
    /// </summary>
    public required double AreaHectares { get; init; }
}
