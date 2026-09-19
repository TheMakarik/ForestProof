namespace ForestProof.Backend.Domain.ChangeZones;

/// <summary>
/// Статус установления причины изменения.
/// </summary>
public enum CauseStatus
{
    /// <summary>
    /// Причина подтверждена совокупностью сигналов.
    /// </summary>
    Confirmed,

    /// <summary>
    /// Причина вероятна по одному сигналу.
    /// </summary>
    Probable,

    /// <summary>
    /// Причина не установлена.
    /// </summary>
    Unknown
}
