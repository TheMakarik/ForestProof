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
    /// Находит доказательства для каждой зоны по растру GFC.
    /// </summary>
    /// <param name="zones">Зоны изменений.</param>
    /// <param name="agbGrid">Сетка, в которой заданы пиксели зон.</param>
    /// <param name="gfc">Растр Hansen GFC в той же CRS.</param>
    /// <returns>Доказательства и статус причины по каждой зоне.</returns>
    IReadOnlyList<ChangeZoneEvidence> Analyze(
        IReadOnlyList<ChangeZone> zones,
        RasterGrid agbGrid,
        GfcWindow gfc);
}
