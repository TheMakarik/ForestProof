namespace ForestProof.Backend.Domain.Analysis;

/// <summary>
/// Запрос на расчёт по территории и периоду.
/// </summary>
public sealed record AnalysisRequest
{
    /// <summary>
    /// Идентификатор AOI.
    /// </summary>
    public required string AoiId { get; init; }

    /// <summary>
    /// Начальный год периода (t₀).
    /// </summary>
    public required int StartYear { get; init; }

    /// <summary>
    /// Конечный год периода (t₁).
    /// </summary>
    public required int EndYear { get; init; }
}
