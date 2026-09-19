namespace ForestProof.Backend.Domain.Report;

/// <summary>
/// Сформированный отчёт по территории и периоду в нескольких представлениях.
/// </summary>
public sealed record ReportResult
{
    /// <summary>
    /// Человекочитаемый отчёт в формате HTML.
    /// </summary>
    public required string Html { get; init; }

    /// <summary>
    /// Машинночитаемая сериализация сводки расчёта.
    /// </summary>
    public required string Json { get; init; }

    /// <summary>
    /// Манифест отчёта с метаданными и перечнем подтверждений.
    /// </summary>
    public required string ManifestJson { get; init; }

    /// <summary>
    /// Отчёт в формате PDF.
    /// </summary>
    public required byte[] Pdf { get; init; }
}
