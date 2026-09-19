using ForestProof.Backend.Domain.Geometry;

namespace ForestProof.Backend.Domain.Raster;

/// <summary>
/// Готовое изменение биомассы ESA CCI Change.
/// </summary>
public sealed record ChangeWindow
{
    /// <summary>
    /// Геотрансформация и размеры растровой сетки.
    /// </summary>
    public required RasterGrid Grid { get; init; }

    /// <summary>
    /// Разность AGB, т/га; null — пропуск.
    /// </summary>
    public required IReadOnlyList<double?> AgbDifference { get; init; }

    /// <summary>
    /// Стандартное отклонение разности, т/га; null — пропуск.
    /// </summary>
    public required IReadOnlyList<double?> StandardDeviation { get; init; }

    /// <summary>
    /// Код качества разности; null — пропуск.
    /// </summary>
    public required IReadOnlyList<int?> QualityFlag { get; init; }
}
