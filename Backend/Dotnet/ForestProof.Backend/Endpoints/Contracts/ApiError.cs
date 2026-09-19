namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Описание ошибки обработки запроса.
/// </summary>
public sealed record ApiError
{
    /// <summary>
    /// Текст ошибки.
    /// </summary>
    public required string Message { get; init; }
}
