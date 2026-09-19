namespace ForestProof.Backend.Domain.Enums;

/// <summary>
/// Статус доступности потенциальных единиц.
/// </summary>
public enum UnitStatus
{
    /// <summary>
    /// Расчёт выполнен, единицы доступны.
    /// </summary>
    Available,

    /// <summary>
    /// Расчёт допустим, но результат нулевой.
    /// </summary>
    Zero,

    /// <summary>
    /// Расчёт недоступен из-за невыполненных условий.
    /// </summary>
    Unavailable
}
