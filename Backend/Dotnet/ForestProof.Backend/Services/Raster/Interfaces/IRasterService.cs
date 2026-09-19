using ForestProof.Backend.Domain.Raster;

namespace ForestProof.Backend.Services.Raster.Interfaces;

/// <summary>
/// Читает растры биомассы ESA CCI Biomass.
/// </summary>
public interface IRasterService
{
    /// <summary>
    /// Читает AGB и AGB_SD за указанный год.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI (папка набора данных).</param>
    /// <param name="year">Год растра.</param>
    /// <returns>Сетку, значения AGB и стандартного отклонения, число валидных пикселей.</returns>
    BiomassWindow ReadBiomass(string aoiId, int year);

    /// <summary>
    /// Читает растровые слои Hansen GFC: древесный покров и год потери.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI (папка набора данных).</param>
    /// <returns>Сетку, древесный покров и год потери по пикселям.</returns>
    GfcWindow ReadGfc(string aoiId);

    /// <summary>
    /// Читает выбранные каналы произвольного растра набора данных.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI (папка набора данных).</param>
    /// <param name="relativePath">Путь к растру относительно папки AOI.</param>
    /// <param name="bandIndexes">Номера каналов (с 1).</param>
    /// <returns>Сетку и значения запрошенных каналов.</returns>
    RasterBandsWindow ReadBands(string aoiId, string relativePath, IReadOnlyList<int> bandIndexes);

    /// <summary>
    /// Сэмплирует дату гари MODIS MCD64A1 в точке WGS 84.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <param name="longitude">Долгота точки, градусы.</param>
    /// <param name="latitude">Широта точки, градусы.</param>
    /// <returns>Порядковый день года гари; null — гари нет или точка вне растра.</returns>
    double? SampleModisBurnDate(string aoiId, double longitude, double latitude);

    /// <summary>
    /// Возвращает год растра гарей MODIS для AOI.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <returns>Год наблюдения; null — файл гарей не найден.</returns>
    int? GetModisBurnYear(string aoiId);

    /// <summary>
    /// Переводит точку WGS 84 в пиксель растра набора данных.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <param name="relativePath">Путь к растру относительно папки AOI.</param>
    /// <param name="longitude">Долгота точки, градусы.</param>
    /// <param name="latitude">Широта точки, градусы.</param>
    /// <returns>Строка и столбец пикселя; null — точка вне растра.</returns>
    (int Row, int Column)? TransformToPixel(
        string aoiId,
        string relativePath,
        double longitude,
        double latitude);

    /// <summary>
    /// Читает готовое изменение биомассы ESA CCI Change.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI (папка набора данных).</param>
    /// <returns>Сетку, разность AGB, стандартное отклонение и код качества.</returns>
    ChangeWindow ReadChange(string aoiId);
}
