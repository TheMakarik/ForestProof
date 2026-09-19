namespace ForestProof.Backend.Persistence.Enums;

/// <summary>
/// Статус установления причины изменения.
/// </summary>
public enum ZoneCauseStatus
{
    /// <summary>
    /// Подтверждено.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Вероятно.
    /// </summary>
    Likely,

    /// <summary>
    /// Не установлено.
    /// </summary>
    Undetermined
}
