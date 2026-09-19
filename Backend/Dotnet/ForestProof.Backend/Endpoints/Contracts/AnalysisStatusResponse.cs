namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Сведения о статусе запуска анализа по территории и периоду.
/// </summary>
public sealed record AnalysisStatusResponse
{
    /// <summary>
    /// Идентификатор запуска расчёта.
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// Текстовое представление статуса запуска.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Версия методики расчёта.
    /// </summary>
    public required string MethodVersion { get; init; }

    /// <summary>
    /// Версия используемого набора данных.
    /// </summary>
    public required string DataVersion { get; init; }

    /// <summary>
    /// Момент формирования результата.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Хеш входных параметров запроса.
    /// </summary>
    public required string InputHash { get; init; }

    /// <summary>
    /// Начальный год периода (t₀).
    /// </summary>
    public required int StartYear { get; init; }

    /// <summary>
    /// Конечный год периода (t₁).
    /// </summary>
    public required int EndYear { get; init; }

    /// <summary>
    /// Предупреждения расчёта.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }

    /// <summary>
    /// Прогресс выполнения расчёта в диапазоне от 0 до 1.
    /// </summary>
    public required double Progress { get; init; }

    /// <summary>
    /// Ссылка на журнал (логи) расчёта; может отсутствовать.
    /// </summary>
    public required string? LogReference { get; init; }
}
