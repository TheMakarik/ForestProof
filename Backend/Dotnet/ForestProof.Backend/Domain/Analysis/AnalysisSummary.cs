using ForestProof.Backend.Domain.Baseline;
using ForestProof.Backend.Domain.Carbon;
using ForestProof.Backend.Domain.Uncertainty;
using ForestProof.Backend.Domain.Units;

namespace ForestProof.Backend.Domain.Analysis;

/// <summary>
/// Итог расчёта по территории и периоду.
/// </summary>
public sealed record AnalysisSummary
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
    /// Предупреждения расчёта.
    /// </summary>
    public required IReadOnlyList<string> Warnings { get; init; }
}
