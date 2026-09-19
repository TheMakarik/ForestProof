namespace ForestProof.Backend.Domain.Enums;

/// <summary>
/// Причина нулевого или недоступного результата расчёта единиц.
/// </summary>
public enum UnitBlockReason
{
    /// <summary>
    /// Блокировки нет.
    /// </summary>
    None,

    /// <summary>
    /// Расчётная площадь равна нулю.
    /// </summary>
    ZeroArea,

    /// <summary>
    /// Период не положителен.
    /// </summary>
    NonPositivePeriod,

    /// <summary>
    /// Отсутствуют обязательные данные.
    /// </summary>
    MissingData,

    /// <summary>
    /// Покрытие неполное.
    /// </summary>
    IncompleteCoverage,

    /// <summary>
    /// Базовая линия отсутствует.
    /// </summary>
    MissingBaseline,

    /// <summary>
    /// Результат относительно baseline не положителен.
    /// </summary>
    NonPositiveResult,

    /// <summary>
    /// Неопределённость сравнима с результатом или больше него.
    /// </summary>
    UncertaintyExceedsResult
}
