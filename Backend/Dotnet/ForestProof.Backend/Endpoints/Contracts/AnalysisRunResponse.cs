namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Компактные сведения о сохранённом запуске анализа.
/// </summary>
public sealed record AnalysisRunResponse
{
    /// <summary>
    /// Идентификатор запуска расчёта.
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// Статус запуска.
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
    /// Момент создания запуска.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Хэш входных параметров.
    /// </summary>
    public required string InputHash { get; init; }

    /// <summary>
    /// Идентификатор AOI; может отсутствовать при пользовательском полигоне.
    /// </summary>
    public string? AoiId { get; init; }

    /// <summary>
    /// Начальный год периода (t₀).
    /// </summary>
    public required int StartYear { get; init; }

    /// <summary>
    /// Конечный год периода (t₁).
    /// </summary>
    public required int YearEnd { get; init; }

    /// <summary>
    /// Расчётная площадь, га.
    /// </summary>
    public required double PolygonAreaHectares { get; init; }

    /// <summary>
    /// Потенциальные единицы (Q); null — расчёт недоступен.
    /// </summary>
    public int? Units { get; init; }

    /// <summary>
    /// Статус расчёта единиц.
    /// </summary>
    public string? QStatus { get; init; }

    /// <summary>
    /// Предупреждения расчёта.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }
}
