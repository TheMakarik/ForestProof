using ForestProof.Backend.Domain.Baseline;
using ForestProof.Backend.Domain.Carbon;
using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Domain.Provenance;
using ForestProof.Backend.Domain.Uncertainty;
using ForestProof.Backend.Domain.Units;

namespace ForestProof.Backend.Domain.Analysis;

/// <summary>
/// Итог расчёта по территории и периоду.
/// </summary>
public sealed record AnalysisSummary
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
    /// Хэш входных параметров для проверки повторяемости.
    /// </summary>
    public required string InputHash { get; init; }

    /// <summary>
    /// Файловые активы источников, использованных в расчёте.
    /// </summary>
    public required IReadOnlyList<SourceAsset> SourceAssets { get; init; }

    /// <summary>
    /// Статус завершённого расчёта.
    /// </summary>
    public required RunStatus Status { get; init; }

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
    public required IReadOnlyList<YearlyCarbonStock> YearlySeries { get; init; }

    /// <summary>
    /// Изменение запаса и CO₂-эквивалент.
    /// </summary>
    public required ProjectChange Change { get; init; }

    /// <summary>
    /// Сценарный диапазон неопределённости.
    /// </summary>
    public required UncertaintyRange Uncertainty { get; init; }

    /// <summary>
    /// Результат базовой линии.
    /// </summary>
    public required BaselineResult Baseline { get; init; }

    /// <summary>
    /// Потенциальные единицы.
    /// </summary>
    public required UnitResult Units { get; init; }

    /// <summary>
    /// Зоны изменений биомассы с подтверждениями.
    /// </summary>
    public required IReadOnlyList<ChangeZone> ChangeZones { get; init; }

    /// <summary>
    /// Доказательства и статус причины по зонам изменений.
    /// </summary>
    public required IReadOnlyList<ChangeZoneEvidence> ChangeZoneEvidence { get; init; }

    /// <summary>
    /// Средняя разность AGB по CCI Change для контрольной пары 2019–2020, т/га.
    /// </summary>
    public required double? CciChangeMeanTonnesPerHectare { get; init; }

    /// <summary>
    /// Предупреждения расчёта.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }
}
