namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Запрос на создание расчёта по территории и периоду.
/// </summary>
public sealed record CreateAnalysisRequest
{
    /// <summary>
    /// Идентификатор AOI из каталога; может быть не задан при пользовательском полигоне.
    /// </summary>
    public string? AoiId { get; init; }

    /// <summary>
    /// Пользовательский полигон в GeoJSON (WGS 84); имеет приоритет над AoiId.
    /// </summary>
    public string? PolygonGeoJson { get; init; }

    /// <summary>
    /// Начальный год периода (t₀).
    /// </summary>
    public required int StartYear { get; init; }

    /// <summary>
    /// Конечный год периода (t₁).
    /// </summary>
    public required int EndYear { get; init; }
}
