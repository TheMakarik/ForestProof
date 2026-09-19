using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace ForestProof.Testing.Analysis;

public sealed class AnalysisPipelineTests
{
    [Fact]
    public void Run_WhenManualExample_Returns395Units()
    {
        // Arrange
        var rasterService = A.Fake<IRasterService>();
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2023)).Returns(CreateWindow(100, 0.3));
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2024)).Returns(CreateWindow(104, 0.3));
        var geometryService = CreateGeometryService(pixelAreaHectares: 100);
        var systemUnderTests = CreatePipeline(rasterService, geometryService);

        // Act
        var summary = systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_TEST_01",
            StartYear = 2023,
            EndYear = 2024
        });

        // Assert
        summary.Change.ProjectEmission.Should().BeApproximately(-689.3333333333, 1e-6);
        summary.Uncertainty.HalfWidth.Should().BeApproximately(103.4, 1e-6);
        summary.Units.Units.Should().Be(395);
        summary.YearlySeries.Should().HaveCount(2);
        summary.YearlySeries[0].Coverage.Should().BeApproximately(1, 1e-9);
    }

    [Fact]
    public void Run_WhenCoverageIncomplete_ReturnsUnavailableUnits()
    {
        // Arrange
        var rasterService = A.Fake<IRasterService>();
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2023)).Returns(CreateWindow(100, 0.3));
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2024)).Returns(CreateWindow(104, 0.3));
        var geometryService = CreateGeometryService(pixelAreaHectares: 90);
        var systemUnderTests = CreatePipeline(rasterService, geometryService);

        // Act
        var summary = systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_TEST_01",
            StartYear = 2023,
            EndYear = 2024
        });

        // Assert
        summary.Units.Status.Should().Be(UnitStatus.Unavailable);
        summary.Units.Units.Should().BeNull();
        summary.Warnings.Should().NotBeEmpty();
    }

    [Fact]
    public void Run_WhenPeriodNotPositive_Throws()
    {
        // Arrange
        var rasterService = A.Fake<IRasterService>();
        var geometryService = CreateGeometryService(pixelAreaHectares: 100);
        var systemUnderTests = CreatePipeline(rasterService, geometryService);

        // Act
        Action act = () => systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_TEST_01",
            StartYear = 2024,
            EndYear = 2024
        });

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Run_WhenCoverageIncomplete_ComputesBaselineOverPolygonArea()
    {
        // Arrange
        var rasterService = A.Fake<IRasterService>();
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2023)).Returns(CreateWindow(100, 0.3));
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2024)).Returns(CreateWindow(104, 0.3));
        var geometryService = CreateGeometryService(pixelAreaHectares: 50);
        var systemUnderTests = CreatePipeline(rasterService, geometryService);

        // Act
        var summary = systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_TEST_01",
            StartYear = 2023,
            EndYear = 2024
        });

        // Assert
        summary.Baseline.BaselineEmission.Should().BeApproximately(-100 * 0.47 * 44.0 / 12.0, 1e-6);
    }

    [Fact]
    public void Run_WhenStandardDeviationMissing_BlocksUnitsAsIncompleteCoverage()
    {
        // Arrange
        var rasterService = A.Fake<IRasterService>();
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2023)).Returns(CreateWindow(100, null));
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2024)).Returns(CreateWindow(104, null));
        var geometryService = CreateGeometryService(pixelAreaHectares: 100);
        var systemUnderTests = CreatePipeline(rasterService, geometryService);

        // Act
        var summary = systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_TEST_01",
            StartYear = 2023,
            EndYear = 2024
        });

        // Assert
        summary.Units.Status.Should().Be(UnitStatus.Unavailable);
        summary.Units.Reason.Should().Be(UnitBlockReason.IncompleteCoverage);
    }

    private static AnalysisPipeline CreatePipeline(IRasterService rasterService, IGeometryService geometryService)
    {
        var options = TestCalculationOptions.Create();

        var aoiCatalogReader = A.Fake<IAoiCatalogReader>();
        A.CallTo(() => aoiCatalogReader.ReadAreas()).Returns(
        [
            new AreaOfInterest
            {
                Id = "RU_TEST_01",
                Name = "Тестовый участок",
                Region = "Регион",
                AnalysisStartYear = 2023,
                AnalysisEndYear = 2024,
                AreaHectares = 100,
                SelectionRole = "контрольный участок",
                ProjectStatus = "исследовательский участок",
                BoundingBox = new AoiBoundingBox { West = 0, South = 0, East = 0.01, North = 0.01 },
                BaselineId = "HIST-1"
            }
        ]);
        A.CallTo(() => aoiCatalogReader.ReadGeometry("RU_TEST_01")).Returns(CreateSquare());

        var baselineReader = A.Fake<IBaselineReader>();
        A.CallTo(() => baselineReader.ReadBaseline("RU_TEST_01")).Returns(new BaselineRecord
        {
            BaselineId = "HIST-1",
            AoiId = "RU_TEST_01",
            YearStart = 2019,
            YearEnd = 2020,
            Pool = "AGB",
            ReferenceMean2015 = 45.12,
            ReferenceMean2019 = 47.0,
            HistoricalRate = 0.47
        });

        return new AnalysisPipeline(
            aoiCatalogReader,
            baselineReader,
            rasterService,
            geometryService,
            new CarbonCalculator(options),
            new UncertaintyCalculator(options),
            new BaselineCalculator(options),
            new UnitCalculator(options),
            options);
    }

    private static IGeometryService CreateGeometryService(double pixelAreaHectares)
    {
        var geometryService = A.Fake<IGeometryService>();
        A.CallTo(() => geometryService.CalculatePixelAreas(A<NtsGeometry>._, A<RasterGrid>._)).Returns(new PixelAreaResult
        {
            PolygonAreaHectares = 100,
            TotalAreaHectares = pixelAreaHectares,
            Pixels =
            [
                new PixelIntersection { Row = 0, Column = 0, AreaHectares = pixelAreaHectares }
            ],
            Warnings = []
        });

        return geometryService;
    }

    private static BiomassWindow CreateWindow(double biomass, double? standardDeviation) => new()
    {
        Grid = new RasterGrid
        {
            OriginLongitude = 0,
            OriginLatitude = 0.01,
            PixelWidthDegrees = 0.01,
            PixelHeightDegrees = 0.01,
            Width = 1,
            Height = 1
        },
        Biomass = [biomass],
        StandardDeviation = [standardDeviation],
        ValidPixelCount = 1
    };

    private static Polygon CreateSquare()
    {
        var factory = new GeometryFactory();
        return factory.CreatePolygon(
        [
            new Coordinate(0, 0),
            new Coordinate(0.01, 0),
            new Coordinate(0.01, 0.01),
            new Coordinate(0, 0.01),
            new Coordinate(0, 0)
        ]);
    }
}
