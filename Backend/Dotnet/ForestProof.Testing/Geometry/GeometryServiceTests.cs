namespace ForestProof.Testing.Geometry;

public sealed class GeometryServiceTests
{
    private readonly IOptions<GeometryOptions> _options;

    public GeometryServiceTests()
    {
        _options = TestGeometryOptions.Create();
    }

    [Fact]
    public void CalculatePixelAreas_WhenPolygonCoversWholePixel_ReturnsPixelArea()
    {
        // Arrange
        var systemUnderTests = new GeometryService(_options);
        var grid = CreateGrid();

        // Act
        var result = systemUnderTests.CalculatePixelAreas(CreateSquare(0, 0, 0.01, 0.01), grid);

        // Assert
        result.Warnings.Should().BeEmpty();
        result.Pixels.Should().ContainSingle();
        result.Pixels[0].Row.Should().Be(0);
        result.Pixels[0].Column.Should().Be(0);
        result.TotalAreaHectares.Should().BeApproximately(result.PolygonAreaHectares, 1e-9);
        result.TotalAreaHectares.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculatePixelAreas_WhenPolygonCoversHalfPixel_ReturnsHalfArea()
    {
        // Arrange
        var systemUnderTests = new GeometryService(_options);
        var grid = CreateGrid();
        var whole = systemUnderTests.CalculatePixelAreas(CreateSquare(0, 0, 0.01, 0.01), grid);

        // Act
        var half = systemUnderTests.CalculatePixelAreas(CreateSquare(0, 0, 0.005, 0.01), grid);

        // Assert
        half.TotalAreaHectares.Should().BeApproximately(whole.TotalAreaHectares / 2, 1e-9);
    }

    [Fact]
    public void CalculatePixelAreas_WhenPolygonInsideSinglePixel_ReturnsPolygonArea()
    {
        // Arrange
        var systemUnderTests = new GeometryService(_options);
        var grid = CreateGrid();

        // Act
        var result = systemUnderTests.CalculatePixelAreas(CreateSquare(0.004, 0.004, 0.006, 0.006), grid);

        // Assert
        result.Pixels.Should().ContainSingle();
        result.TotalAreaHectares.Should().BeApproximately(result.PolygonAreaHectares, 1e-9);
    }

    [Fact]
    public void CalculatePixelAreas_WhenPolygonSpansFourPixels_SumsToPolygonArea()
    {
        // Arrange
        var systemUnderTests = new GeometryService(_options);
        var grid = CreateGrid(0.02);

        // Act
        var result = systemUnderTests.CalculatePixelAreas(CreateSquare(0, 0, 0.02, 0.02), grid);

        // Assert
        result.Pixels.Should().HaveCount(4);
        result.TotalAreaHectares.Should().BeApproximately(result.PolygonAreaHectares, 1e-9);
    }

    [Fact]
    public void CalculatePixelAreas_WhenPolygonOutsideGrid_ReturnsEmpty()
    {
        // Arrange
        var systemUnderTests = new GeometryService(_options);
        var grid = CreateGrid();

        // Act
        var result = systemUnderTests.CalculatePixelAreas(CreateSquare(1, 1, 1.01, 1.01), grid);

        // Assert
        result.Pixels.Should().BeEmpty();
        result.TotalAreaHectares.Should().Be(0);
    }

    [Fact]
    public void CalculatePixelAreas_WhenPolygonSelfIntersects_FixesAndWarns()
    {
        // Arrange
        var systemUnderTests = new GeometryService(_options);
        var grid = CreateGrid();
        var factory = new GeometryFactory();
        var bowtie = factory.CreatePolygon(
        [
            new Coordinate(0, 0),
            new Coordinate(0.01, 0.01),
            new Coordinate(0.01, 0),
            new Coordinate(0, 0.01),
            new Coordinate(0, 0)
        ]);

        // Act
        var result = systemUnderTests.CalculatePixelAreas(bowtie, grid);

        // Assert
        result.Warnings.Should().NotBeEmpty();
        result.TotalAreaHectares.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculatePixelAreas_WhenHighLatitude_DoesNotAssumePixelEqualsHectare()
    {
        // Arrange
        var systemUnderTests = new GeometryService(_options);
        var equator = systemUnderTests.CalculatePixelAreas(CreateSquare(0, 0, 0.01, 0.01), CreateGrid(0.01));

        // Act
        var north = systemUnderTests.CalculatePixelAreas(CreateSquare(0, 60, 0.01, 60.01), CreateGrid(60.01));

        // Assert
        north.TotalAreaHectares.Should().BeLessThan(equator.TotalAreaHectares * 0.6);
    }

    [Fact]
    public void CalculatePixelAreas_WhenCoordinatesOutsideWgs84Range_Throws()
    {
        // Arrange
        var systemUnderTests = new GeometryService(_options);

        // Act
        Action act = () => systemUnderTests.CalculatePixelAreas(
            CreateSquare(1_000_000, 0, 1_000_001, 1),
            CreateGrid());

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculatePixelAreas_WhenPixelSizeNotPositive_Throws()
    {
        // Arrange
        var systemUnderTests = new GeometryService(_options);
        var grid = new RasterGrid
        {
            OriginLongitude = 0,
            OriginLatitude = 0.01,
            PixelWidthDegrees = 0,
            PixelHeightDegrees = 0.01,
            Width = 10,
            Height = 10
        };

        // Act
        Action act = () => systemUnderTests.CalculatePixelAreas(CreateSquare(0, 0, 0.01, 0.01), grid);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    private static RasterGrid CreateGrid(double originLatitude = 0.01) => new()
    {
        OriginLongitude = 0,
        OriginLatitude = originLatitude,
        PixelWidthDegrees = 0.01,
        PixelHeightDegrees = 0.01,
        Width = 10,
        Height = 10
    };

    private static Polygon CreateSquare(double minLongitude, double minLatitude, double maxLongitude, double maxLatitude)
    {
        var factory = new GeometryFactory();
        return factory.CreatePolygon(
        [
            new Coordinate(minLongitude, minLatitude),
            new Coordinate(maxLongitude, minLatitude),
            new Coordinate(maxLongitude, maxLatitude),
            new Coordinate(minLongitude, maxLatitude),
            new Coordinate(minLongitude, minLatitude)
        ]);
    }
}
