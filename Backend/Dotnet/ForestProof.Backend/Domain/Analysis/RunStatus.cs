namespace ForestProof.Backend.Domain.Analysis;

/// <summary>
/// Статус завершённого расчёта.
/// </summary>
public enum RunStatus
{
    /// <summary>
    /// Все обязательные результаты доступны.
    /// </summary>
    Complete,

    /// <summary>
    /// Часть результатов доступна, но есть предупреждения.
    /// </summary>
    Partial,

    /// <summary>
    /// Расчёт потенциальных единиц заблокирован.
    /// </summary>
    UnitsUnavailable
}
