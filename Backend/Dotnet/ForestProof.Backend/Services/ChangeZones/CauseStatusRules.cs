using ForestProof.Backend.Domain.ChangeZones;

namespace ForestProof.Backend.Services.ChangeZones;

/// <summary>
/// Определяет статус причины по набору подтверждающих наблюдений.
/// </summary>
public static class CauseStatusRules
{
    /// <summary>
    /// Сопоставляет набор наблюдений статусу причины.
    /// </summary>
    /// <param name="evidenceTypes">Найденные наблюдения.</param>
    /// <returns>Статус причины.</returns>
    public static CauseStatus Evaluate(IReadOnlyCollection<EvidenceType> evidenceTypes)
    {
        var causeSignals = evidenceTypes.Count(type => type != EvidenceType.Gfc);

        return causeSignals switch
        {
            >= 2 => CauseStatus.Confirmed,
            1 => CauseStatus.Probable,
            _ => CauseStatus.Unknown
        };
    }
}
