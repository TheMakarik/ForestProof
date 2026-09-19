namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Связная зона изменения биомассы.
/// </summary>
public sealed record ChangeZoneResponse
{
    /// <summary>
    /// Порядковый идентификатор зоны.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Площадь зоны, га.
    /// </summary>
    public required double AreaHectares { get; init; }

    /// <summary>
    /// Вклад зоны в изменение запаса углерода, т C.
    /// </summary>
    public required double ContributionToDeltaCarbon { get; init; }
}
