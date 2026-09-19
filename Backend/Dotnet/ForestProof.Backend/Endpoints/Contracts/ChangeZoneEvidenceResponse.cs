namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Доказательства и статус причины для зоны изменений.
/// </summary>
public sealed record ChangeZoneEvidenceResponse
{
    /// <summary>
    /// Идентификатор зоны.
    /// </summary>
    public required int ZoneId { get; init; }

    /// <summary>
    /// Найденные подтверждающие наблюдения.
    /// </summary>
    public required IReadOnlyList<string> EvidenceTypes { get; init; }

    /// <summary>
    /// Статус причины.
    /// </summary>
    public required string CauseStatus { get; init; }
}
