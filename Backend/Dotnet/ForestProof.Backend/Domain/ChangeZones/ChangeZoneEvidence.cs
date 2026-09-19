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
    /// Годы потери покрова по GFC в зоне.
    /// </summary>
    public required IReadOnlyList<int> GfcLossYears { get; init; }

    /// <summary>
    /// Годы всех найденных подтверждающих наблюдений.
    /// </summary>
    public required IReadOnlyList<int> EvidenceYears { get; init; }

    /// <summary>
    /// Статус причины.
    /// </summary>
    public required CauseStatus CauseStatus { get; init; }

    /// <summary>
    /// Текст «что можно утверждать / чего нельзя» для интерфейса и отчёта.
    /// </summary>
    public required string Interpretation { get; init; }
}
