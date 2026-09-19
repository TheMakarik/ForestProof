namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Запрос на создание проекта.
/// </summary>
public sealed record CreateProjectRequest
{
    /// <summary>
    /// Название проекта; обязательное поле.
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
