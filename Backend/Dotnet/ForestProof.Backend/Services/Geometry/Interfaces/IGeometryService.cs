using ForestProof.Backend.Domain.Geometry;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace ForestProof.Backend.Services.Geometry.Interfaces;

/// <summary>
/// Считает площади пересечения пикселей растровой сетки с полигоном.
/// </summary>
public interface IGeometryService
{
    /// <summary>
    /// Перепроецирует полигон в целевую метрическую CRS и считает площадь пересечения с каждым пикселем.
    /// </summary>
    /// <param name="polygon">Полигон AOI в исходной системе координат (WGS 84).</param>
    /// <param name="grid">Геотрансформация растровой сетки в WGS 84.</param>
    /// <returns>Площадь полигона, суммарную площадь пересечений, список пикселей и предупреждения.</returns>
    PixelAreaResult CalculatePixelAreas(NtsGeometry polygon, RasterGrid grid);
}
