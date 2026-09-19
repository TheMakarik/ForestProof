namespace ForestProof.Backend.Domain.Aoi;

/// <summary>
/// Метаданные анализируемой территории (AOI).
/// </summary>
public sealed record AreaOfInterest
{
    /// <summary>
    /// Идентификатор AOI.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Название территории.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Регион.
    /// </summary>
    public required string Region { get; init; }

    /// <summary>
    /// Начальный год анализа.
    /// </summary>
    public required int AnalysisStartYear { get; init; }

    /// <summary>
    /// Конечный год анализа.
    /// </summary>
    public required int AnalysisEndYear { get; init; }

    /// <summary>
    /// Площадь территории, га.
    /// </summary>
    public required double AreaHectares { get; init; }

    /// <summary>
    /// Роль участка в наборе (например, контрольный участок).
    /// </summary>
    public required string SelectionRole { get; init; }

    /// <summary>
    /// Статус проекта.
    /// </summary>
    public required string ProjectStatus { get; init; }

    /// <summary>
    /// Ограничивающий прямоугольник AOI.
    /// </summary>
    public required AoiBoundingBox BoundingBox { get; init; }

    /// <summary>
    /// Идентификатор базовой линии AOI.
    /// </summary>
    public required string BaselineId { get; init; }
}
