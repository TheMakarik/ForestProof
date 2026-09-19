using ForestProof.Backend.Domain.Geometry;

namespace ForestProof.Backend.Domain.Raster;

/// <summary>
/// Значения Hansen GFC по пикселям: древесный покров и год потери.
/// </summary>
public sealed record GfcWindow
{
    /// <summary>
    /// Геотрансформация и размеры растровой сетки.
    /// </summary>
    public required RasterGrid Grid { get; init; }

    /// <summary>
    /// Древесный покров 2000 года, проценты; null — пропуск.
    /// </summary>
    public required IReadOnlyList<int?> TreeCover { get; init; }

    /// <summary>
    /// Год потери покрова (год − 2000); null — пропуск, 0 — потери нет.
    /// </summary>
    public required IReadOnlyList<int?> LossYear { get; init; }
}
