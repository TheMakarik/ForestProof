using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace ForestProof.Testing.Analysis;

public sealed class AnalysisValidationTests
{
    [Fact]
    public void Run_WhenStartYearNotLessThanEndYear_Throws()
    {
        // Arrange
        var rasterService = A.Fake<IRasterService>();
        var geometryService = CreateGeometryService(polygonAreaHectares: 100, pixelAreaHectares: 100);
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
    public void Run_WhenPeriodOutsideAllowedRange_Throws()
    {
        // Arrange
        var rasterService = A.Fake<IRasterService>();
        var geometryService = CreateGeometryService(polygonAreaHectares: 100, pixelAreaHectares: 100);
        var systemUnderTests = CreatePipeline(rasterService, geometryService);

        // Act
        Action act = () => systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_TEST_01",
            StartYear = 2010,
            EndYear = 2012
        });

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Run_WhenPolygonAreaExceedsLimit_Throws()
    {
        // Arrange
        var rasterService = A.Fake<IRasterService>();
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2023)).Returns(CreateWindow(100, 0.3));
        A.CallTo(() => rasterService.ReadBiomass("RU_TEST_01", 2024)).Returns(CreateWindow(104, 0.3));
        var geometryService = CreateGeometryService(polygonAreaHectares: 3_000_000, pixelAreaHectares: 100);
        var systemUnderTests = CreatePipeline(rasterService, geometryService);

        // Act
        Action act = () => systemUnderTests.Run(new AnalysisRequest
        {
            AoiId = "RU_TEST_01",
            StartYear = 2023,
            EndYear = 2024
        });

        // Assert
        act.Should().Throw<InvalidOperationException>();
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

        A.CallTo(() => rasterService.ReadGfc("RU_TEST_01")).Returns(CreateGfcWindow());
        A.CallTo(() => rasterService.ReadChange("RU_TEST_01")).Returns(CreateChangeWindow());

        var sourceCatalogService = A.Fake<ISourceCatalogService>();
        A.CallTo(() => sourceCatalogService.ReadCatalog(A<string>._)).Returns(Array.Empty<SourceAsset>());

        return new AnalysisPipeline(
            aoiCatalogReader,
            baselineReader,
            rasterService,
            geometryService,
            new CarbonCalculator(options),
            new UncertaintyCalculator(options),
            new BaselineCalculator(options),
            new UnitCalculator(options),
            new ChangeZoneDetector(options),
            new ChangeZoneEvidenceAnalyzer(
                rasterService,
                A.Fake<ISpectralIndexService>(),
                A.Fake<ISentinelSceneSelector>(),
                options),
            sourceCatalogService,
            options);
    }

    private static IGeometryService CreateGeometryService(double polygonAreaHectares, double pixelAreaHectares)
    {
        var geometryService = A.Fake<IGeometryService>();
        A.CallTo(() => geometryService.CalculatePixelAreas(A<NtsGeometry>._, A<RasterGrid>._)).Returns(new PixelAreaResult
        {
            PolygonAreaHectares = polygonAreaHectares,
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

    private static ChangeWindow CreateChangeWindow() => new()
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
        AgbDifference = [0],
        StandardDeviation = [0],
        QualityFlag = [0]
    };

    private static GfcWindow CreateGfcWindow(int lossYear = 0) => new()
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
        TreeCover = [50],
        LossYear = [lossYear]
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
