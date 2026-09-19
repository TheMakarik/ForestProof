namespace ForestProof.Backend.Domain.Aoi;

/// <summary>
/// Ограничивающий прямоугольник AOI в WGS 84.
/// </summary>
public sealed record AoiBoundingBox
{
    /// <summary>
    /// Западная граница, градусы.
    /// </summary>
    public required double West { get; init; }

    /// <summary>
    /// Южная граница, градусы.
    /// </summary>
    public required double South { get; init; }

    /// <summary>
    /// Восточная граница, градусы.
    /// </summary>
    public required double East { get; init; }

    /// <summary>
    /// Северная граница, градусы.
    /// </summary>
    public required double North { get; init; }
}
