namespace ForestProof.Testing.Integration;

public sealed class SpectralIntegrationTests
{
    private static readonly string Root = TestDataset.Locate();

    [Fact]
    public void Calculate_WhenVologdaScene_ReturnsExpectedGrid()
    {
        // Arrange
        var systemUnderTests = new SpectralIndexService(
            new RasterService(TestDataOptions.Create(Root)),
            TestCalculationOptions.Create());

        // Act
        var window = systemUnderTests.Calculate(
            "RU_VOLOGDA_02",
            "Sentinel2/S2B_37VEF_20240816_0_L2A_reflectance.tif",
            "Sentinel2/S2B_37VEF_20240816_0_L2A_SCL.tif");

        // Assert
        window.Grid.Width.Should().Be(188);
        window.Grid.Height.Should().Be(228);
        window.ValidPixelCount.Should().Be(42864);
        window.Ndvi
            .Where(value => value.HasValue)
            .Should().OnlyContain(value => value >= -1 && value <= 1);
    }
}
