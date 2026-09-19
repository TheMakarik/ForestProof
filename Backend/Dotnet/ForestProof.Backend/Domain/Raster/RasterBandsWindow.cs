using ForestProof.Backend.Domain.Geometry;

namespace ForestProof.Backend.Domain.Raster;

/// <summary>
/// Значения нескольких каналов растра на общей сетке.
/// </summary>
public sealed record RasterBandsWindow
{
    /// <summary>
    /// Геотрансформация и размеры растровой сетки.
    /// </summary>
    public required RasterGrid Grid { get; init; }

    /// <summary>
    /// Значения каналов в порядке запроса; null — пропуск.
    /// </summary>
    public required IReadOnlyList<IReadOnlyList<double?>> Bands { get; init; }
}
