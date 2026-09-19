using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace ForestProof.Testing.Spectral;

public sealed class SpectralIndexServiceTests : IDisposable
{
    private readonly TempDataset _dataset = new();
    private readonly IOptions<CalculationOptions> _options = TestCalculationOptions.Create();

    public SpectralIndexServiceTests()
    {
        GdalInitializer.EnsureInitialized();
    }

    [Fact]
    public void Calculate_WhenPixelIsVegetation_ReturnsExpectedIndices()
    {
        // Arrange
        CreateReflectance(
            "RU_TEST_01/S2_scene_reflectance.tif",
            b02: 0.1, b03: 0.2, b04: 0.3, b8a: 0.5, b11: 0.4, b12: 0.25);
        CreateScl("RU_TEST_01/S2_scene_SCL.tif", 4);
        var systemUnderTests = new SpectralIndexService(new RasterService(TestDataOptions.Create(_dataset.RootPath)), _options);

        // Act
        var window = systemUnderTests.Calculate("RU_TEST_01", "S2_scene_reflectance.tif", "S2_scene_SCL.tif");

        // Assert
        window.ValidPixelCount.Should().Be(1);
        window.Ndvi[0].Should().BeApproximately((0.5 - 0.3) / (0.5 + 0.3), 1e-9);
        window.Ndwi[0].Should().BeApproximately((0.2 - 0.5) / (0.2 + 0.5), 1e-9);
        window.Nbr[0].Should().BeApproximately((0.5 - 0.25) / (0.5 + 0.25), 1e-9);
    }

    [Fact]
    public void Calculate_WhenSclNotAllowed_MasksPixel()
    {
        // Arrange
        CreateReflectance(
            "RU_TEST_01/S2_scene_reflectance.tif",
            b02: 0.1, b03: 0.2, b04: 0.3, b8a: 0.5, b11: 0.4, b12: 0.25);
        CreateScl("RU_TEST_01/S2_scene_SCL.tif", 0);
        var systemUnderTests = new SpectralIndexService(new RasterService(TestDataOptions.Create(_dataset.RootPath)), _options);

        // Act
        var window = systemUnderTests.Calculate("RU_TEST_01", "S2_scene_reflectance.tif", "S2_scene_SCL.tif");

        // Assert
        window.ValidPixelCount.Should().Be(0);
        window.Ndvi[0].Should().BeNull();
        window.Ndwi[0].Should().BeNull();
        window.Nbr[0].Should().BeNull();
    }

    [Fact]
    public void Calculate_WhenDenominatorZero_ReturnsNullIndex()
    {
        // Arrange
        CreateReflectance(
            "RU_TEST_01/S2_scene_reflectance.tif",
            b02: 0.1, b03: 0.2, b04: 0.0, b8a: 0.0, b11: 0.4, b12: 0.25);
        CreateScl("RU_TEST_01/S2_scene_SCL.tif", 4);
        var systemUnderTests = new SpectralIndexService(new RasterService(TestDataOptions.Create(_dataset.RootPath)), _options);

        // Act
        var window = systemUnderTests.Calculate("RU_TEST_01", "S2_scene_reflectance.tif", "S2_scene_SCL.tif");

        // Assert
        window.Ndvi[0].Should().BeNull();
    }

    [Fact]
    public void CalculateDnbr_WhenBothObservationsPresent_ReturnsDifference()
    {
        // Arrange
        var systemUnderTests = new SpectralIndexService(
            new RasterService(TestDataOptions.Create(_dataset.RootPath)),
            _options);
        var grid = new RasterGrid
        {
            OriginLongitude = 0,
            OriginLatitude = 0.001,
            PixelWidthDegrees = 0.001,
            PixelHeightDegrees = 0.001,
            Width = 1,
            Height = 1
        };
        var before = new SpectralIndexWindow
        {
            Grid = grid,
            Ndvi = [null],
            Ndwi = [null],
            Nbr = [0.33],
            ValidPixelCount = 1
        };
        var after = new SpectralIndexWindow
        {
            Grid = grid,
            Ndvi = [null],
            Ndwi = [null],
            Nbr = [0.10],
            ValidPixelCount = 1
        };

        // Act
        var dnbr = systemUnderTests.CalculateDnbr(before, after);

        // Assert
        dnbr[0].Should().BeApproximately(0.23, 1e-9);
    }

    public void Dispose()
    {
        _dataset.Dispose();
    }

    private void CreateReflectance(
        string relativePath,
        double b02,
        double b03,
        double b04,
        double b8a,
        double b11,
        double b12)
    {
        var path = Path.Join(_dataset.RootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var driver = Gdal.GetDriverByName("GTiff");
        using var dataset = driver.Create(path, 1, 1, 6, DataType.GDT_Float64, null);
        dataset.SetGeoTransform([0, 0.001, 0, 0.001, 0, -0.001]);

        double[][] bands = [[b02], [b03], [b04], [b8a], [b11], [b12]];
        for (var i = 0; i < bands.Length; i++)
            dataset.GetRasterBand(i + 1).WriteRaster(0, 0, 1, 1, bands[i], 1, 1, 0, 0);

        dataset.FlushCache();
    }

    private void CreateScl(string relativePath, byte value)
    {
        var path = Path.Join(_dataset.RootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var driver = Gdal.GetDriverByName("GTiff");
        using var dataset = driver.Create(path, 1, 1, 1, DataType.GDT_Byte, null);
        dataset.SetGeoTransform([0, 0.001, 0, 0.001, 0, -0.001]);
        dataset.GetRasterBand(1).WriteRaster(0, 0, 1, 1, [value], 1, 1, 0, 0);

        dataset.FlushCache();
    }
}
