namespace ForestProof.Testing.Integration;

public sealed class RasterIntegrationTests
{
    private static readonly string Root = TestDataset.Locate();

    [Fact]
    public void ReadBiomass_WhenVologda2019_ReturnsExpectedGrid()
    {
        // Arrange
        var systemUnderTests = new RasterService(TestDataOptions.Create(Root));

        // Act
        var window = systemUnderTests.ReadBiomass("RU_VOLOGDA_02", 2019);

        // Assert
        window.Grid.Width.Should().Be(73);
        window.Grid.Height.Should().Be(46);
        window.ValidPixelCount.Should().Be(3358);
        window.Biomass.Should().Contain(value => value == 0);
    }

    [Fact]
    public void ReadGfc_WhenVologda_ReturnsExpectedGridWithLossYears()
    {
        // Arrange
        var systemUnderTests = new RasterService(TestDataOptions.Create(Root));

        // Act
        var window = systemUnderTests.ReadGfc("RU_VOLOGDA_02");

        // Assert
        window.Grid.Width.Should().Be(256);
        window.Grid.Height.Should().Be(160);
        window.LossYear.Should().Contain(value => value.HasValue);
    }
}
