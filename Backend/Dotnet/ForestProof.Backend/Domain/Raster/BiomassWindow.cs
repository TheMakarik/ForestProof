using ForestProof.Backend.Domain.Geometry;

namespace ForestProof.Backend.Domain.Raster;

/// <summary>
/// Значения биомассы и её стандартного отклонения по пикселям растровой сетки.
/// </summary>
public sealed record BiomassWindow
{
    /// <summary>
    /// Геотрансформация и размеры растровой сетки.
    /// </summary>
    public required RasterGrid Grid { get; init; }

    /// <summary>
    /// AGB по пикселям, т/га, в порядке строка-за-строкой; null — пропуск.
    /// </summary>
    public required IReadOnlyList<double?> Biomass { get; init; }

    /// <summary>
    /// Стандартное отклонение AGB по пикселям, т/га; null — пропуск.
    /// </summary>
    public required IReadOnlyList<double?> StandardDeviation { get; init; }

    /// <summary>
    /// Число пикселей с валидной AGB.
    /// </summary>
    public required int ValidPixelCount { get; init; }
}
