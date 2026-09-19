namespace ForestProof.Testing.ChangeZones;

public sealed class ChangeZoneEvidenceAnalyzerTests
{
    [Fact]
    public void Analyze_WhenGfcLossPresent_AddsGfcEvidence()
    {
        // Arrange
        var systemUnderTests = new ChangeZoneEvidenceAnalyzer();

        // Act
        var evidence = systemUnderTests.Analyze([CreateZone()], CreateGrid(), CreateGfc(lossYear: 5));

        // Assert
        evidence.Should().ContainSingle();
        evidence[0].EvidenceTypes.Should().Contain(EvidenceType.Gfc);
        evidence[0].CauseStatus.Should().Be(CauseStatus.Unknown);
    }

    [Fact]
    public void Analyze_WhenGfcLossAbsent_ReturnsNoEvidence()
    {
        // Arrange
        var systemUnderTests = new ChangeZoneEvidenceAnalyzer();

        // Act
        var evidence = systemUnderTests.Analyze([CreateZone()], CreateGrid(), CreateGfc(lossYear: 0));

        // Assert
        evidence[0].EvidenceTypes.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_WhenZoneOutsideGfcGrid_ReturnsNoEvidence()
    {
        // Arrange
        var systemUnderTests = new ChangeZoneEvidenceAnalyzer();

        // Act
        var evidence = systemUnderTests.Analyze([CreateZone(row: 100, column: 100)], CreateGrid(), CreateGfc(lossYear: 5));

        // Assert
        evidence[0].EvidenceTypes.Should().BeEmpty();
    }

    private static ChangeZone CreateZone(int row = 0, int column = 0) => new()
    {
        Id = 1,
        AreaHectares = 1,
        ContributionToDeltaCarbon = 1,
        PixelCount = 1,
        Pixels = [new ChangePixel { Row = row, Column = column, AreaHectares = 1, BiomassChange = 20 }]
    };

    private static RasterGrid CreateGrid() => new()
    {
        OriginLongitude = 0,
        OriginLatitude = 0.01,
        PixelWidthDegrees = 0.01,
        PixelHeightDegrees = 0.01,
        Width = 1,
        Height = 1
    };

    private static GfcWindow CreateGfc(int lossYear) => new()
    {
        Grid = CreateGrid(),
        TreeCover = [50],
        LossYear = [lossYear]
    };
}
