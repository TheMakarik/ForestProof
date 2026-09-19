using ForestProof.Backend.Domain.ChangeZones;

namespace ForestProof.Backend.Services.ChangeZones;

/// <summary>
/// Определяет статус причины по набору подтверждающих наблюдений.
/// </summary>
public static class CauseStatusRules
{
    /// <summary>
    /// Сопоставляет набор наблюдений статусу причины с учётом временного окна.
    /// </summary>
    /// <param name="evidenceTypes">Найденные наблюдения.</param>
    /// <param name="evidenceYears">Годы найденных наблюдений.</param>
    /// <param name="startYear">Начальный год периода.</param>
    /// <param name="endYear">Конечный год периода.</param>
    /// <returns>Статус причины.</returns>
    public static CauseStatus Evaluate(
        IReadOnlyCollection<EvidenceType> evidenceTypes,
        IReadOnlyCollection<int> evidenceYears,
        int startYear,
        int endYear)
    {
        var temporalWindowMatches = evidenceYears.Count == 0 ||
            evidenceYears.All(year => year >= startYear && year <= endYear);

        if (!temporalWindowMatches)
            return CauseStatus.Unknown;

        var causeSignals = evidenceTypes.Count(type => type != EvidenceType.Gfc);

        return causeSignals switch
        {
            >= 2 => CauseStatus.Confirmed,
            1 => CauseStatus.Probable,
            _ => CauseStatus.Unknown
        };
    }

    /// <summary>
    /// Возвращает текст «что можно утверждать / чего нельзя» для статуса причины.
    /// </summary>
    /// <param name="status">Статус причины.</param>
    /// <returns>Формулировка для интерфейса и отчёта.</returns>
    public static string Interpretation(CauseStatus status) => status switch
    {
        CauseStatus.Confirmed =>
            "Изменение подтверждено совокупностью независимых наблюдений; причина указана с ограничениями.",
        CauseStatus.Probable =>
            "Вероятное нарушение или восстановление; требуется дополнительное подтверждение.",
        _ =>
            "Причина не установлена: данных мало или сигналы конфликтуют."
    };
}
