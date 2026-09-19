namespace ForestProof.Testing.Data;

public sealed class AoiCatalogReaderTests : IDisposable
{
    private readonly TempDataset _dataset = new();
    private readonly IOptions<DataOptions> _options;

    public AoiCatalogReaderTests()
    {
        _dataset.WriteFile(
            "areas.csv",
            "aoi_id,name,region,analysis_start_year,analysis_end_year,area_ha,selection_role,project_status,bbox_west,bbox_south,bbox_east,bbox_north,baseline_id\n" +
            "RU_TEST_01,Тестовый участок,Регион,2019,2024,1750.5,контрольный участок,исследовательский участок,32.91,56.59,32.974,56.63,HIST-1\n");

        _dataset.WriteFile(
            "areas.geojson",
            """
            {
              "type": "FeatureCollection",
              "features": [
                {
                  "type": "Feature",
                  "id": "RU_TEST_01",
                  "properties": { "aoi_id": "RU_TEST_01" },
                  "geometry": {
                    "type": "Polygon",
                    "coordinates": [[[32.91,56.59],[32.974,56.59],[32.974,56.63],[32.91,56.63],[32.91,56.59]]]
                  }
                }
              ]
            }
            """);

        _options = TestDataOptions.Create(_dataset.RootPath);
    }

    [Fact]
    public void ReadAreas_WhenCatalogHasRow_ReturnsMetadata()
    {
        // Arrange
        var systemUnderTests = new AoiCatalogReader(_options);

        // Act
        var areas = systemUnderTests.ReadAreas();

        // Assert
        areas.Should().ContainSingle();
        var area = areas.Single();
        area.Id.Should().Be("RU_TEST_01");
        area.AnalysisStartYear.Should().Be(2019);
        area.AnalysisEndYear.Should().Be(2024);
        area.AreaHectares.Should().BeApproximately(1750.5, 1e-9);
        area.BaselineId.Should().Be("HIST-1");
        area.BoundingBox.West.Should().BeApproximately(32.91, 1e-9);
        area.BoundingBox.North.Should().BeApproximately(56.63, 1e-9);
    }

    [Fact]
    public void ReadGeometry_WhenAoiExists_ReturnsPolygon()
    {
        // Arrange
        var systemUnderTests = new AoiCatalogReader(_options);

        // Act
        var geometry = systemUnderTests.ReadGeometry("RU_TEST_01");

        // Assert
        geometry.GeometryType.Should().Be("Polygon");
        geometry.Area.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ReadGeometry_WhenAoiMissing_Throws()
    {
        // Arrange
        var systemUnderTests = new AoiCatalogReader(_options);

        // Act
        Action act = () => systemUnderTests.ReadGeometry("UNKNOWN");

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    public void Dispose()
    {
        _dataset.Dispose();
    }
}
