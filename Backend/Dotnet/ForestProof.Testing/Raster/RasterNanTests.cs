using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace ForestProof.Testing.Raster;

public sealed class RasterNanTests : IDisposable
{
    private readonly TempDataset _dataset = new();
    private readonly IOptions<DataOptions> _options;

    public RasterNanTests()
    {
        GdalInitializer.EnsureInitialized();
        _options = TestDataOptions.Create(_dataset.RootPath);
    }

    [Fact]
    public void ReadBiomass_WhenValueIsNaN_ReturnsNullAndExcludesFromValidCount()
    {
        // Arrange
        CreateRaster("RU_TEST_01/CCI_Biomass_2019.tif", [double.NaN, 10, 20, 30]);
        var systemUnderTests = new RasterService(_options);

        // Act
        var window = systemUnderTests.ReadBiomass("RU_TEST_01", 2019);

        // Assert
        window.Biomass[0].Should().BeNull();
        window.ValidPixelCount.Should().Be(3);
    }

    public void Dispose()
    {
        _dataset.Dispose();
    }

    private void CreateRaster(string relativePath, double[] biomass)
    {
        var path = Path.Join(_dataset.RootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var driver = Gdal.GetDriverByName("GTiff");
        using var dataset = driver.Create(path, 2, 2, 2, DataType.GDT_Float64, null);
        dataset.SetGeoTransform([10, 0.001, 0, 20, 0, -0.001]);
        dataset.SetProjection(CreateWkt(4326));

        WriteBand(dataset.GetRasterBand(1), biomass);
        WriteBand(dataset.GetRasterBand(2), [0, 1, 2, 3]);

        dataset.FlushCache();
    }

    private static string CreateWkt(int epsg)
    {
        using var spatialReference = new OSGeo.OSR.SpatialReference("");
        spatialReference.ImportFromEPSG(epsg);
        spatialReference.ExportToWkt(out var wkt, null);
        return wkt;
    }

    private static void WriteBand(Band band, double[] values)
    {
        band.WriteRaster(0, 0, 2, 2, values, 2, 2, 0, 0);
    }
}
