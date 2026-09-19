using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Domain.Raster;

namespace ForestProof.Backend.Services.ChangeZones.Interfaces;

/// <summary>
/// Сопоставляет зонам изменений подтверждающие наблюдения и статус причины.
/// </summary>
public interface IChangeZoneEvidenceAnalyzer
{
    /// <summary>
    /// Находит доказательства для каждой зоны по растрам GFC и MODIS.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <param name="zones">Зоны изменений.</param>
    /// <param name="agbGrid">Сетка, в которой заданы пиксели зон.</param>
    /// <param name="gfc">Растр Hansen GFC в той же CRS.</param>
    /// <param name="startYear">Начальный год периода.</param>
    /// <param name="endYear">Конечный год периода.</param>
    /// <param name="useExtendedSclClasses">Использовать расширенную SCL-маску 4–7.</param>
    /// <returns>Доказательства и статус причины по каждой зоне.</returns>
    IReadOnlyList<ChangeZoneEvidence> Analyze(
        string aoiId,
        IReadOnlyList<ChangeZone> zones,
        RasterGrid agbGrid,
        GfcWindow gfc,
        int startYear,
        int endYear,
        bool useExtendedSclClasses);
}
