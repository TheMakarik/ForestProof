namespace ForestProof.Backend.Domain.Geometry;

/// <summary>
/// Результат расчёта площадей пересечения пикселей с полигоном.
/// </summary>
public sealed record PixelAreaResult
{
    /// <summary>
    /// Площадь полигона в целевой метрической CRS, га.
    /// </summary>
    public required double PolygonAreaHectares { get; init; }

    /// <summary>
    /// Суммарная расчётная площадь пересечений, га (A = Σ aᵢ).
    /// </summary>
    public required double TotalAreaHectares { get; init; }

    /// <summary>
    /// Пиксели, пересекающие полигон, с площадями пересечения.
    /// </summary>
    public required IReadOnlyList<PixelIntersection> Pixels { get; init; }

    /// <summary>
    /// Предупреждения о геометрии, например об исправленных самопересечениях.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }
}
