namespace ForestProof.Backend.Persistence.Enums;

/// <summary>
/// Тип подтверждающего наблюдения зоны изменений.
/// </summary>
public enum ZoneEvidenceType
{
    /// <summary>
    /// Hansen GFC.
    /// </summary>
    Gfc,

    /// <summary>
    /// Sentinel-2.
    /// </summary>
    Sentinel2,

    /// <summary>
    /// MODIS.
    /// </summary>
    Modis,

    /// <summary>
    /// ESA CCI Change.
    /// </summary>
    CciChange
}
