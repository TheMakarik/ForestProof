namespace ForestProof.Testing.Integration;

public sealed class DataReadersIntegrationTests
{
    private static readonly string Root = TestDataset.Locate();

    [Fact]
    public void ReadAreas_WhenRealDataset_ReturnsFourAreas()
    {
        // Arrange
        var systemUnderTests = new AoiCatalogReader(TestDataOptions.Create(Root));

        // Act
        var areas = systemUnderTests.ReadAreas();

        // Assert
        areas.Should().HaveCount(4);
        areas.Should().Contain(area => area.Id == "RU_VOLOGDA_02");
    }

    [Fact]
    public void ReadGeometry_WhenVologda_ReturnsPolygonWithArea()
    {
        // Arrange
        var systemUnderTests = new AoiCatalogReader(TestDataOptions.Create(Root));

        // Act
        var geometry = systemUnderTests.ReadGeometry("RU_VOLOGDA_02");

        // Assert
        geometry.GeometryType.Should().Be("Polygon");
        geometry.Area.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ReadBaseline_WhenVologda_ReturnsReferenceMean2019()
    {
        // Arrange
        var systemUnderTests = new BaselineReader(TestDataOptions.Create(Root));

        // Act
        var baseline = systemUnderTests.ReadBaseline("RU_VOLOGDA_02");

        // Assert
        baseline.AoiId.Should().Be("RU_VOLOGDA_02");
        baseline.ReferenceMean2019.Should().BeApproximately(75.714302086, 1e-9);
    }

    [Fact]
    public void ReadParameters_WhenRealDataset_ReturnsTwelveParameters()
    {
        // Arrange
        var systemUnderTests = new ParametersReader(TestDataOptions.Create(Root));

        // Act
        var parameters = systemUnderTests.ReadParameters();

        // Assert
        parameters.Should().HaveCount(12);
        parameters.Should().ContainKey("CF_AGB");
    }
}
