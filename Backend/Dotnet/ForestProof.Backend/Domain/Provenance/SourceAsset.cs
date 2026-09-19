namespace ForestProof.Backend.Domain.Provenance;

/// <summary>
/// Описание исходного файла набора данных и его контрольной суммы.
/// </summary>
public sealed record SourceAsset
{
    /// <summary>
    /// Идентификатор территории, к которой относится файл.
    /// </summary>
    public required string AoiId { get; init; }

    /// <summary>
    /// Путь к файлу относительно корня набора данных.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Контрольная сумма SHA-256 содержимого файла.
    /// </summary>
    public required string Sha256 { get; init; }

    /// <summary>
    /// Размер файла в байтах.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Версия продукта-источника.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Дата получения или создания файла.
    /// </summary>
    public string? RetrievedAt { get; init; }
}
