namespace ForestProof.Backend.Domain.ChangeZones;

/// <summary>
/// Источник подтверждающего наблюдения для зоны изменений.
/// </summary>
public enum EvidenceType
{
    /// <summary>
    /// Hansen GFC — факт и год потери покрова (не причина).
    /// </summary>
    Gfc,

    /// <summary>
    /// MODIS MCD64A1 — сигнал возможной гари.
    /// </summary>
    Modis,

    /// <summary>
    /// Sentinel-2 — спектральный сигнал нарушения или восстановления.
    /// </summary>
    Sentinel2
}
