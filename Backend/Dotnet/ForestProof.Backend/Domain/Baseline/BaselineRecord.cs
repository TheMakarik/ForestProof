namespace ForestProof.Backend.Domain.Baseline;

/// <summary>
/// Строка методической базовой линии из baseline.csv.
/// </summary>
public sealed record BaselineRecord
{
    /// <summary>
    /// Идентификатор базовой линии.
    /// </summary>
    public required string BaselineId { get; init; }

    /// <summary>
    /// Идентификатор AOI.
    /// </summary>
    public required string AoiId { get; init; }

    /// <summary>
    /// Начальный год строки сценария.
    /// </summary>
    public required int YearStart { get; init; }

    /// <summary>
    /// Конечный год строки сценария.
    /// </summary>
    public required int YearEnd { get; init; }

    /// <summary>
    /// Учитываемый пул углерода (по условиям кейса — AGB).
    /// </summary>
    public required string Pool { get; init; }

    /// <summary>
    /// Сценарный удельный запас в историческом 2015 году, т C/га.
    /// </summary>
    public required double ReferenceMean2015 { get; init; }

    /// <summary>
    /// Сценарный удельный запас в опорном 2019 году, т C/га.
    /// </summary>
    public required double ReferenceMean2019 { get; init; }

    /// <summary>
    /// Исторический годовой темп изменения, т C/га/год.
    /// </summary>
    public required double HistoricalRate { get; init; }
}
