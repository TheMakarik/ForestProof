namespace ForestProof.Backend.Domain.ChangeZones;

using NetTopologySuite.Geometries;

/// <summary>
/// Связная зона изменения биомассы.
/// </summary>
public sealed record ChangeZone
{
    /// <summary>
    /// Порядковый идентификатор зоны.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Геометрия зоны в WGS 84.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Geometry? Geometry { get; init; }

    /// <summary>
    /// Площадь зоны, га.
    /// </summary>
    public required double AreaHectares { get; init; }

    /// <summary>
    /// Вклад зоны в изменение запаса углерода, т C.
    /// </summary>
    public required double ContributionToDeltaCarbon { get; init; }

    /// <summary>
    /// Число пикселей в зоне.
    /// </summary>
    public required int PixelCount { get; init; }

    /// <summary>
    /// Пиксели зоны с площадями и изменением биомассы.
    /// </summary>
    public required IReadOnlyList<ChangePixel> Pixels { get; init; }
}
