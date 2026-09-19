namespace ForestProof.Backend.Domain.Geometry;

/// <summary>
/// Геотрансформация растровой сетки в WGS 84 (EPSG:4326).
/// </summary>
public sealed record RasterGrid
{
    /// <summary>
    /// Долгота левого верхнего угла сетки, градусы.
    /// </summary>
    public required double OriginLongitude { get; init; }

    /// <summary>
    /// Широта левого верхнего угла сетки, градусы.
    /// </summary>
    public required double OriginLatitude { get; init; }

    /// <summary>
    /// Ширина пикселя, градусы.
    /// </summary>
    public required double PixelWidthDegrees { get; init; }

    /// <summary>
    /// Высота пикселя, градусы (положительная).
    /// </summary>
    public required double PixelHeightDegrees { get; init; }

    /// <summary>
    /// Число столбцов сетки.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Число строк сетки.
    /// </summary>
    public required int Height { get; init; }
}
