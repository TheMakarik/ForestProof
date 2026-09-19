namespace ForestProof.Backend.Persistence.Enums;

/// <summary>
/// Статус запуска анализа.
/// </summary>
public enum AnalysisStatus
{
    /// <summary>
    /// Запрос создан.
    /// </summary>
    Draft,

    /// <summary>
    /// Проверяются геометрия и данные.
    /// </summary>
    Validating,

    /// <summary>
    /// Выполняется расчёт.
    /// </summary>
    Running,

    /// <summary>
    /// Расчёт завершён.
    /// </summary>
    Complete,

    /// <summary>
    /// Часть результатов доступна.
    /// </summary>
    Partial,

    /// <summary>
    /// Расчёт единиц заблокирован.
    /// </summary>
    UnitsUnavailable,

    /// <summary>
    /// Ошибка расчёта.
    /// </summary>
    Failed
}
