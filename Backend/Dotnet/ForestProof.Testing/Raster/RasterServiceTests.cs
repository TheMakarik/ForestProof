using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace ForestProof.Testing.Raster;

public sealed class RasterServiceTests : IDisposable
{
    private readonly TempDataset _dataset = new();
    private readonly IOptions<DataOptions> _options;

    public RasterServiceTests()
    {
        GdalInitializer.EnsureInitialized();
        _options = TestDataOptions.Create(_dataset.RootPath);
    }

    [Fact]
    public void ReadBiomass_WhenGridPresent_ReturnsGeotransformAndValues()
    {
        // Arrange
        CreateRaster("RU_TEST_01/CCI_Biomass_2019.tif", [0, 10, 20, 30], [0, 1, 2, 3], noData: null);
        var systemUnderTests = new RasterService(_options);

        // Act
        var window = systemUnderTests.ReadBiomass("RU_TEST_01", 2019);

        // Assert
        window.Grid.Width.Should().Be(2);
        window.Grid.Height.Should().Be(2);
        window.Grid.OriginLongitude.Should().BeApproximately(10, 1e-9);
        window.Grid.OriginLatitude.Should().BeApproximately(20, 1e-9);
        window.Grid.PixelWidthDegrees.Should().BeApproximately(0.001, 1e-12);
        window.Grid.PixelHeightDegrees.Should().BeApproximately(0.001, 1e-12);
        window.ValidPixelCount.Should().Be(4);
        window.Biomass.Should().Equal(new double?[] { 0, 10, 20, 30 });
    }

    [Fact]
    public void ReadBiomass_WhenZeroBiomass_KeepsZeroAsValid()
    {
        // Arrange
        CreateRaster("RU_TEST_01/CCI_Biomass_2019.tif", [0, 10, 20, 30], [0, 1, 2, 3], noData: null);
        var systemUnderTests = new RasterService(_options);

        // Act
        var window = systemUnderTests.ReadBiomass("RU_TEST_01", 2019);

        // Assert
        window.Biomass[0].Should().Be(0);
        window.StandardDeviation[0].Should().Be(0);
    }

    [Fact]
    public void ReadBiomass_WhenBandHasNoDataTag_ReturnsNullForNoData()
    {
        // Arrange
        CreateRaster("RU_TEST_01/CCI_Biomass_2019.tif", [-9999, 10, 20, 30], [1, 1, 2, 3], noData: -9999);
        var systemUnderTests = new RasterService(_options);

        // Act
        var window = systemUnderTests.ReadBiomass("RU_TEST_01", 2019);

        // Assert
        window.Biomass[0].Should().BeNull();
        window.ValidPixelCount.Should().Be(3);
    }

    [Fact]
    public void ReadBiomass_WhenCrsNotWgs84_Throws()
    {
        // Arrange
        CreateRaster("RU_TEST_01/CCI_Biomass_2019.tif", [0, 10, 20, 30], [0, 1, 2, 3], noData: null, epsg: 3857);
        var systemUnderTests = new RasterService(_options);

        // Act
        Action act = () => systemUnderTests.ReadBiomass("RU_TEST_01", 2019);

        // Assert
        act.Should().Throw<InvalidDataException>();
    }

    public void Dispose()
    {
        _dataset.Dispose();
    }

    private void CreateRaster(
        string relativePath,
        double[] biomass,
        double[] standardDeviation,
        double? noData,
        int epsg = 4326)
    {
        var path = Path.Join(_dataset.RootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var driver = Gdal.GetDriverByName("GTiff");
        using var dataset = driver.Create(path, 2, 2, 2, DataType.GDT_Float64, null);
        dataset.SetGeoTransform([10, 0.001, 0, 20, 0, -0.001]);
        dataset.SetProjection(CreateWkt(epsg));

        WriteBand(dataset.GetRasterBand(1), biomass, noData);
        WriteBand(dataset.GetRasterBand(2), standardDeviation, noData: null);

        dataset.FlushCache();
    }

    private static string CreateWkt(int epsg)
    {
        using var spatialReference = new OSGeo.OSR.SpatialReference("");
        spatialReference.ImportFromEPSG(epsg);
        spatialReference.ExportToWkt(out var wkt, null);
        return wkt;
    }

    private static void WriteBand(Band band, double[] values, double? noData)
    {
        band.WriteRaster(0, 0, 2, 2, values, 2, 2, 0, 0);
        if (noData is { } value)
            band.SetNoDataValue(value);
    }
}
