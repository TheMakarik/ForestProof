using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Domain.Geometry;

namespace ForestProof.Backend.Services.ChangeZones.Interfaces;

/// <summary>
/// Выделяет связные зоны изменений биомассы по порогу.
/// </summary>
public interface IChangeZoneDetector
{
    /// <summary>
    /// Строит связные компоненты пикселей с изменением выше порога и фильтрует их по площади.
    /// </summary>
    /// <param name="pixels">Пиксели с изменением биомассы.</param>
    /// <param name="grid">Растровая сетка для построения геометрии зон.</param>
    /// <returns>Зоны изменений с геометрией, площадью и вкладом в ΔC.</returns>
    IReadOnlyList<ChangeZone> Detect(IReadOnlyList<ChangePixel> pixels, RasterGrid grid);
}
