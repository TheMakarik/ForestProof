using ForestProof.Backend.Domain.Aoi;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace ForestProof.Backend.Services.Data.Interfaces;

/// <summary>
/// Читает каталог территорий и их геометрию из набора данных.
/// </summary>
public interface IAoiCatalogReader
{
    /// <summary>
    /// Читает метаданные всех AOI.
    /// </summary>
    /// <returns>Список территорий с метаданными.</returns>
    IReadOnlyCollection<AreaOfInterest> ReadAreas();

    /// <summary>
    /// Читает геометрию AOI в WGS 84.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <returns>Геометрия полигона AOI.</returns>
    /// <exception cref="KeyNotFoundException">Если AOI отсутствует в areas.geojson.</exception>
    NtsGeometry ReadGeometry(string aoiId);
}
