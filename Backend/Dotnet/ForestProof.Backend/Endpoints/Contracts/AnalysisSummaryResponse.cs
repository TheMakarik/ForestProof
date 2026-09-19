namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Плоский ответ с итогом расчёта по территории и периоду.
/// </summary>
public sealed record AnalysisSummaryResponse
{
    /// <summary>
    /// Идентификатор запуска расчёта.
    /// </summary>
    public required string RunId { get; init; }

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
    /// Статус расчёта.
    /// </summary>
    public required string Status { get; init; }

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

    /// <summary>
    /// Площадь полигона, га.
    /// </summary>
    public required double PolygonAreaHectares { get; init; }

    /// <summary>
    /// Годовая динамика запаса и покрытия.
    /// </summary>
    public required IReadOnlyList<YearlySeriesItem> YearlySeries { get; init; }

    /// <summary>
    /// Изменение запаса и CO₂-эквивалент.
    /// </summary>
    public required ChangeResponse Change { get; init; }

    /// <summary>
    /// Сценарный диапазон неопределённости.
    /// </summary>
    public required UncertaintyResponse Uncertainty { get; init; }

    /// <summary>
    /// Результат базовой линии.
    /// </summary>
    public required BaselineResponse Baseline { get; init; }

    /// <summary>
    /// Потенциальные единицы.
    /// </summary>
    public required UnitsResponse Units { get; init; }

    /// <summary>
    /// Зоны изменений биомассы.
    /// </summary>
    public required IReadOnlyList<ChangeZoneResponse> ChangeZones { get; init; }

    /// <summary>
    /// Доказательства и статус причины по зонам изменений.
    /// </summary>
    public required IReadOnlyList<ChangeZoneEvidenceResponse> ChangeZoneEvidence { get; init; }

    /// <summary>
    /// Средняя разность AGB по CCI Change для контрольной пары 2019–2020, т/га.
    /// </summary>
    public required double? CciChangeMeanTonnesPerHectare { get; init; }

    /// <summary>
    /// Предупреждения расчёта.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }
}
