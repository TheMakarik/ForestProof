namespace ForestProof.Backend.Services.Spectral.Interfaces;

/// <summary>
/// Читает каталог сцен Sentinel-2 с метриками качества наблюдений.
/// </summary>
public interface ISceneCatalogReader
{
    /// <summary>
    /// Читает сцены указанного AOI из каталога scenes.csv.
    /// </summary>
    /// <param name="scenesCsvPath">Путь к файлу каталога сцен.</param>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <returns>Сцены AOI; пустая коллекция, если каталог недоступен или не содержит данных AOI.</returns>
    IReadOnlyCollection<SceneCatalogEntry> Read(string scenesCsvPath, string aoiId);
}

/// <summary>
/// Сцена Sentinel-2 с метриками качества из каталога scenes.csv.
/// </summary>
public sealed record SceneCatalogEntry
{
    /// <summary>
    /// Ключ сцены.
    /// </summary>
    public required string SceneKey { get; init; }

    /// <summary>
    /// Идентификатор элемента, совпадающий с именем файла сцены без суффикса растра.
    /// </summary>
    public required string ItemId { get; init; }

    /// <summary>
    /// Дата съёмки.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Облачность сцены в процентах.
    /// </summary>
    public required double CloudPercent { get; init; }

    /// <summary>
    /// Доля пикселей допустимых классов SCL (4–5).
    /// </summary>
    public required double ValidSclFraction { get; init; }

    /// <summary>
    /// Относительный путь к растру отражения.
    /// </summary>
    public required string ReflectancePath { get; init; }

    /// <summary>
    /// Относительный путь к растру SCL.
    /// </summary>
    public required string SclPath { get; init; }
}
