namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Запрос на формирование отчёта по территории и периоду.
/// </summary>
public sealed record CreateReportRequest
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

    /// <summary>
    /// Формат отчёта: "html", "json" или "pdf"; null — PDF по умолчанию.
    /// </summary>
    public string? Format { get; init; }
}
