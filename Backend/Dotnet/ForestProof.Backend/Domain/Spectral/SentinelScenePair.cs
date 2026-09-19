namespace ForestProof.Backend.Domain.Spectral;

/// <summary>
/// Пара сопоставимых сцен Sentinel-2 до и после изменения.
/// </summary>
public sealed record SentinelScenePair
{
    /// <summary>
    /// Имя файла отражения до изменения.
    /// </summary>
    public required string BeforeReflectanceFileName { get; init; }

    /// <summary>
    /// Имя файла SCL до изменения.
    /// </summary>
    public required string BeforeSclFileName { get; init; }

    /// <summary>
    /// Год наблюдения до изменения.
    /// </summary>
    public required int BeforeYear { get; init; }

    /// <summary>
    /// Имя файла отражения после изменения.
    /// </summary>
    public required string AfterReflectanceFileName { get; init; }

    /// <summary>
    /// Имя файла SCL после изменения.
    /// </summary>
    public required string AfterSclFileName { get; init; }

    /// <summary>
    /// Год наблюдения после изменения.
    /// </summary>
    public required int AfterYear { get; init; }
}
