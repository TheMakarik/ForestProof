namespace ForestProof.Backend.Domain.ChangeZones;

/// <summary>
/// Пиксель с изменением биомассы между начальной и конечной датой.
/// </summary>
public sealed record ChangePixel
{
    /// <summary>
    /// Строка пикселя в растровой сетке.
    /// </summary>
    public required int Row { get; init; }

    /// <summary>
    /// Столбец пикселя в растровой сетке.
    /// </summary>
    public required int Column { get; init; }

    /// <summary>
    /// Площадь пересечения пикселя с полигоном, га (aᵢ).
    /// </summary>
    public required double AreaHectares { get; init; }

    /// <summary>
    /// Изменение AGB, т/га (bᵢ,t₁ − bᵢ,t₀).
    /// </summary>
    public required double BiomassChange { get; init; }

    /// <summary>
    /// Признак наличия хотя бы одного подтверждения (GFC, Sentinel-2 или MODIS).
    /// </summary>
    public required bool HasConfirmation { get; init; }
}
