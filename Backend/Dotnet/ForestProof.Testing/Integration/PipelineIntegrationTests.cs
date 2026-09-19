namespace ForestProof.Testing.Integration;

public sealed class PipelineIntegrationTests
{
    private static readonly string Root = TestDataset.Locate();

    [Fact]
    public void Run_WhenVologdaRealDataset_ReturnsExpectedSummary()
    {
        // Arrange
        var systemUnderTests = CreatePipeline();

        // Act
        var summary = systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_VOLOGDA_02",
            StartYear = 2019,
            EndYear = 2024
        });

        // Assert
        summary.PolygonAreaHectares.Should().BeApproximately(1617.7075, 0.01);
        summary.YearlySeries.Should().HaveCount(6);
        summary.YearlySeries[0].Coverage.Should().BeApproximately(1, 1e-6);
        summary.ChangeZones.Should().NotBeEmpty();
        summary.ChangeZoneEvidence.Should().NotBeEmpty();
    }

    [Fact]
    public void Run_WhenMordoviaRealDataset_ReturnsModisEvidence()
    {
        // Arrange
        var systemUnderTests = CreatePipeline();

        // Act
        var summary = systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_MORDOVIA_03",
            StartYear = 2019,
            EndYear = 2024
        });

        // Assert
        summary.ChangeZoneEvidence.Should().Contain(evidence => evidence.EvidenceTypes.Contains(EvidenceType.Modis));
    }

    [Fact]
    public void Run_WhenSampleRequestGeoJson_ReturnsSummaryWithinVologda()
    {
        // Arrange
        var geoJson = File.ReadAllText(Path.Join(Root, "sample_requests.geojson"));
        var systemUnderTests = CreatePipeline();

        // Act
        var summary = systemUnderTests.Run(new AnalysisRequest
        {
            PolygonGeoJson = geoJson,
            StartYear = 2020,
            EndYear = 2024
        });

        // Assert
        summary.PolygonAreaHectares.Should().BeGreaterThan(700);
        summary.YearlySeries.Should().HaveCount(5);
    }

    [Fact]
    public void Run_WhenMordoviaRealDataset_ReturnsSentinelEvidence()
    {
        // Arrange
        var systemUnderTests = CreatePipeline();

        // Act
        var summary = systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_MORDOVIA_03",
            StartYear = 2019,
            EndYear = 2024
        });

        // Assert
        summary.ChangeZoneEvidence.Should()
            .Contain(evidence => evidence.EvidenceTypes.Contains(EvidenceType.Sentinel2));
    }

    private static AnalysisPipeline CreatePipeline()
    {
        var dataOptions = TestDataOptions.Create(Root);
        var geometryOptions = TestGeometryOptions.Create();
        var calculationOptions = TestCalculationOptions.Create();
        var rasterService = new RasterService(dataOptions);

        return new AnalysisPipeline(
            new AoiCatalogReader(dataOptions),
            new BaselineReader(dataOptions),
            rasterService,
            new GeometryService(geometryOptions),
            new CarbonCalculator(calculationOptions),
            new UncertaintyCalculator(calculationOptions),
            new BaselineCalculator(calculationOptions),
            new UnitCalculator(calculationOptions),
            new ChangeZoneDetector(calculationOptions),
            new ChangeZoneEvidenceAnalyzer(
                rasterService,
                new SpectralIndexService(rasterService, calculationOptions),
                new SentinelSceneSelector(dataOptions),
                calculationOptions),
            new SourceCatalogService(dataOptions),
            calculationOptions);
    }
}
