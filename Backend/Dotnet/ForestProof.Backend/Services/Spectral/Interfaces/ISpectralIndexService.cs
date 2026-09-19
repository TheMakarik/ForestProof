using ForestProof.Backend.Domain.Spectral;

namespace ForestProof.Backend.Services.Spectral.Interfaces;

/// <summary>
/// Считает спектральные индексы Sentinel-2 с учётом SCL-маски.
/// </summary>
public interface ISpectralIndexService
{
    /// <summary>
    /// Считает NDVI, NDWI и NBR по валидным пикселям сцены.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <param name="reflectanceFileName">Имя файла отражения (каналы B02, B03, B04, B8A, B11, B12).</param>
    /// <param name="sclFileName">Имя файла SCL.</param>
    /// <returns>Индексы по пикселям и число валидных пикселей.</returns>
    SpectralIndexWindow Calculate(string aoiId, string reflectanceFileName, string sclFileName);

    /// <summary>
    /// Считает NDVI, NDWI и NBR по валидным пикселям с заданными допустимыми SCL-классами.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <param name="reflectanceFileName">Имя файла отражения.</param>
    /// <param name="sclFileName">Имя файла SCL.</param>
    /// <param name="allowedSclClasses">Допустимые классы SCL.</param>
    /// <returns>Индексы по пикселям и число валидных пикселей.</returns>
    SpectralIndexWindow Calculate(
        string aoiId,
        string reflectanceFileName,
        string sclFileName,
        IReadOnlyCollection<int> allowedSclClasses);

    /// <summary>
    /// Считает dNBR как разность NBR между двумя сопоставимыми наблюдениями.
    /// </summary>
    /// <param name="before">Наблюдение до изменения.</param>
    /// <param name="after">Наблюдение после изменения.</param>
    /// <returns>dNBR по пикселям; null — нет данных.</returns>
    IReadOnlyList<double?> CalculateDnbr(SpectralIndexWindow before, SpectralIndexWindow after);
}
