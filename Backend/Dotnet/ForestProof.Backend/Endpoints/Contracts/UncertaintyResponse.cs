namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Сценарный диапазон изменения запаса углерода.
/// </summary>
public sealed record UncertaintyResponse
{
    /// <summary>
    /// Нижняя граница Eproj, т CO₂-экв. (L).
    /// </summary>
    public required double Lower { get; init; }

    /// <summary>
    /// Верхняя граница Eproj, т CO₂-экв. (U).
    /// </summary>
    public required double Upper { get; init; }

    /// <summary>
    /// Максимальное расстояние от Eproj до границы диапазона, т CO₂-экв. (H).
    /// </summary>
    public required double HalfWidth { get; init; }
}
