namespace ForestProof.Testing.ChangeZones;

public sealed class ChangeZoneDetectorTests
{
    private readonly IOptions<CalculationOptions> _options = TestCalculationOptions.Create();

    [Fact]
    public void Detect_WhenPixelsAdjacent_MergesIntoSingleZone()
    {
        // Arrange
        var systemUnderTests = new ChangeZoneDetector(_options);
        ChangePixel[] pixels =
        [
            new() { Row = 0, Column = 0, AreaHectares = 1, BiomassChange = 20, HasConfirmation = true },
            new() { Row = 0, Column = 1, AreaHectares = 1, BiomassChange = 20, HasConfirmation = true }
        ];

        // Act
        var zones = systemUnderTests.Detect(pixels, CreateGrid());

        // Assert
        zones.Should().ContainSingle();
        zones[0].AreaHectares.Should().BeApproximately(2, 1e-9);
        zones[0].PixelCount.Should().Be(2);
        zones[0].ContributionToDeltaCarbon.Should().BeApproximately(20 * 0.47 * 2, 1e-9);
    }

    [Fact]
    public void Detect_WhenPixelsSeparated_ReturnsTwoZones()
    {
        // Arrange
        var systemUnderTests = new ChangeZoneDetector(_options);
        ChangePixel[] pixels =
        [
            new() { Row = 0, Column = 0, AreaHectares = 2, BiomassChange = 20, HasConfirmation = true },
            new() { Row = 2, Column = 2, AreaHectares = 2, BiomassChange = -20, HasConfirmation = true }
        ];

        // Act
        var zones = systemUnderTests.Detect(pixels, CreateGrid());

        // Assert
        zones.Should().HaveCount(2);
    }

    [Fact]
    public void Detect_WhenChangeBelowThreshold_IgnoresPixel()
    {
        // Arrange
        var systemUnderTests = new ChangeZoneDetector(_options);
        ChangePixel[] pixels =
        [
            new() { Row = 0, Column = 0, AreaHectares = 1, BiomassChange = 5, HasConfirmation = true }
        ];

        // Act
        var zones = systemUnderTests.Detect(pixels, CreateGrid());

        // Assert
        zones.Should().BeEmpty();
    }

    [Fact]
    public void Detect_WhenComponentSmallerThanMinArea_IgnoresComponent()
    {
        // Arrange
        var systemUnderTests = new ChangeZoneDetector(_options);
        ChangePixel[] pixels =
        [
            new() { Row = 0, Column = 0, AreaHectares = 0.4, BiomassChange = 20, HasConfirmation = true },
            new() { Row = 0, Column = 1, AreaHectares = 0.4, BiomassChange = 20, HasConfirmation = true }
        ];

        // Act
        var zones = systemUnderTests.Detect(pixels, CreateGrid());

        // Assert
        zones.Should().BeEmpty();
    }

    [Fact]
    public void Detect_WhenComponentAreaEqualsMinimum_IgnoresComponent()
    {
        // Arrange
        var systemUnderTests = new ChangeZoneDetector(_options);
        ChangePixel[] pixels =
        [
            new() { Row = 0, Column = 0, AreaHectares = 1, BiomassChange = 20, HasConfirmation = true }
        ];

        // Act
        var zones = systemUnderTests.Detect(pixels, CreateGrid());

        // Assert
        zones.Should().BeEmpty();
    }

    [Fact]
    public void Detect_WhenComponentHasNoConfirmation_IgnoresComponent()
    {
        // Arrange
        var systemUnderTests = new ChangeZoneDetector(_options);
        ChangePixel[] pixels =
        [
            new() { Row = 0, Column = 0, AreaHectares = 2, BiomassChange = 20, HasConfirmation = false }
        ];

        // Act
        var zones = systemUnderTests.Detect(pixels, CreateGrid());

        // Assert
        zones.Should().BeEmpty();
    }

    private static RasterGrid CreateGrid() => new()
    {
        OriginLongitude = 0,
        OriginLatitude = 0.01,
        PixelWidthDegrees = 0.01,
        PixelHeightDegrees = 0.01,
        Width = 10,
        Height = 10
    };
}
