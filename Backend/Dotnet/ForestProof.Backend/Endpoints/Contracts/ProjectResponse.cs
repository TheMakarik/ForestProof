namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Ответ с данными проекта.
/// </summary>
public sealed record ProjectResponse
{
    /// <summary>
    /// Идентификатор проекта.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Название проекта.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Идентификатор AOI из каталога; может быть не задан.
    /// </summary>
    public string? AoiId { get; init; }

    /// <summary>
    /// Заявленный результат проекта.
    /// </summary>
    public string? ClaimedResult { get; init; }
}
