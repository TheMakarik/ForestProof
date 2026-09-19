namespace ForestProof.Backend.Persistence.Enums;

/// <summary>
/// Статус доступности потенциальных единиц.
/// </summary>
public enum QStatus
{
    /// <summary>
    /// Расчёт выполнен.
    /// </summary>
    Computed,

    /// <summary>
    /// Расчёт допустим, но результат нулевой.
    /// </summary>
    Zero,

    /// <summary>
    /// Расчёт недоступен.
    /// </summary>
    Unavailable
}
