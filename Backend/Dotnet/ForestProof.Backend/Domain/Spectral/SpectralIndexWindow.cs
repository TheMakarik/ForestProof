using ForestProof.Backend.Domain.Geometry;

namespace ForestProof.Backend.Domain.Spectral;

/// <summary>
/// Спектральные индексы Sentinel-2 по валидным пикселям.
/// </summary>
public sealed record SpectralIndexWindow
{
    /// <summary>
    /// Геотрансформация и размеры растровой сетки.
    /// </summary>
    public required RasterGrid Grid { get; init; }

    /// <summary>
    /// NDVI по пикселям; null — пиксель невалиден или знаменатель нулевой.
    /// </summary>
    public required IReadOnlyList<double?> Ndvi { get; init; }

    /// <summary>
    /// NDWI по пикселям; null — пиксель невалиден или знаменатель нулевой.
    /// </summary>
    public required IReadOnlyList<double?> Ndwi { get; init; }

    /// <summary>
    /// NBR по пикселям; null — пиксель невалиден или знаменатель нулевой.
    /// </summary>
    public required IReadOnlyList<double?> Nbr { get; init; }

    /// <summary>
    /// Число валидных пикселей после SCL-маски.
    /// </summary>
    public required int ValidPixelCount { get; init; }
}
