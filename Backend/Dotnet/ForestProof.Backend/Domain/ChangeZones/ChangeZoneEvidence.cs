namespace ForestProof.Backend.Domain.ChangeZones;

/// <summary>
/// Доказательства и статус причины для зоны изменений.
/// </summary>
public sealed record ChangeZoneEvidence
{
    /// <summary>
    /// Идентификатор зоны.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Найденные подтверждающие наблюдения.
    /// </summary>
    public required IReadOnlyList<EvidenceType> EvidenceTypes { get; init; }

    /// <summary>
    /// Статус причины.
    /// </summary>
    public required CauseStatus CauseStatus { get; init; }
}
