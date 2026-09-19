namespace ForestProof.Backend.Domain.Uncertainty;

/// <summary>
/// Сценарный диапазон изменения запаса углерода (L–U) и его полная ширина (H).
/// </summary>
public sealed record UncertaintyRange
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

    /// <summary>
    /// Коэффициент чувствительности k, использованный при расчёте.
    /// </summary>
    public required double SensitivityCoefficient { get; init; }
}
