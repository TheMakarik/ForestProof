using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace ForestProof.Backend.Services.Raster;

/// <summary>
/// Инициализирует нативные библиотеки GDAL один раз за процесс.
/// </summary>
public static class GdalInitializer
{
    private static readonly Lazy<bool> Initialized = new(() =>
    {
        GdalBase.ConfigureAll();
        Gdal.UseExceptions();
        return true;
    });

    /// <summary>
    /// Гарантирует, что GDAL настроен и готов к работе.
    /// </summary>
    public static void EnsureInitialized() => _ = Initialized.Value;
}
