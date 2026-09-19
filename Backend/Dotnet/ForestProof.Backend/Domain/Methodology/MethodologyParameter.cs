namespace ForestProof.Backend.Domain.Methodology;

/// <summary>
/// Методический параметр из parameters.csv.
/// </summary>
public sealed record MethodologyParameter
{
    /// <summary>
    /// Имя параметра.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Значение параметра в исходном виде.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Единица измерения.
    /// </summary>
    public required string Unit { get; init; }

    /// <summary>
    /// Тип параметра (опубликованное значение, правило кейса и т.д.).
    /// </summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Идентификатор источника.
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    /// Локатор в источнике (таблица, уравнение, страница).
    /// </summary>
    public required string Locator { get; init; }

    /// <summary>
    /// Область применимости и ограничения.
    /// </summary>
    public required string Applicability { get; init; }
}
