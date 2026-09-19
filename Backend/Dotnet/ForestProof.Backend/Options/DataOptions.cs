namespace ForestProof.Backend.Options;

/// <summary>
/// Пути и имена файлов исходного набора данных.
/// </summary>
public sealed class DataOptions
{
    /// <summary>
    /// Корневая папка набора данных.
    /// </summary>
    public required string DataRoot { get; init; }

    /// <summary>
    /// Имя файла полигонов AOI в формате GeoJSON.
    /// </summary>
    public required string AreasGeoJsonFileName { get; init; }

    /// <summary>
    /// Имя файла метаданных AOI в формате CSV.
    /// </summary>
    public required string AreasCsvFileName { get; init; }

    /// <summary>
    /// Имя папки с методическими таблицами.
    /// </summary>
    public required string MethodologyDirectoryName { get; init; }

    /// <summary>
    /// Имя файла базовой линии в формате CSV.
    /// </summary>
    public required string BaselineCsvFileName { get; init; }

    /// <summary>
    /// Имя файла параметров в формате CSV.
    /// </summary>
    public required string ParametersCsvFileName { get; init; }

    /// <summary>
    /// Шаблон имени растров биомассы, где {year} — год.
    /// </summary>
    public required string BiomassRasterFileNamePattern { get; init; }

    /// <summary>
    /// Имя растра готового изменения.
    /// </summary>
    public required string ChangeRasterFileName { get; init; }

    /// <summary>
    /// Имя растра Hansen GFC (потеря покрова).
    /// </summary>
    public required string GfcRasterFileName { get; init; }

    /// <summary>
    /// Имя папки локального кэша внешних источников.
    /// </summary>
    public required string CacheDirectoryName { get; init; }

    /// <summary>
    /// Имя папки для сформированных отчётов.
    /// </summary>
    public required string ReportOutputDirectoryName { get; init; }
}
