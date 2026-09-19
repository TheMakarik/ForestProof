using System.Text.Json;
using ForestProof.Backend.Domain.Pricing;
using ForestProof.Backend.Domain.Report;
using ForestProof.Backend.Domain.Uncertainty;
using ForestProof.Backend.Services.Report;

namespace ForestProof.Testing.Report;

public sealed class ReportServiceTests
{
    private readonly IOptions<DataOptions> _options = TestDataOptions.Create("/tmp/forestproof-report");

    [Fact]
    public void Generate_ReturnsHtmlWithKeyFiguresAndDisclaimer()
    {
        // Arrange
        var systemUnderTests = new ReportService(_options);

        // Act
        var result = systemUnderTests.Generate(CreateSummary());

        // Assert
        result.Html.Should().Contain("Q");
        result.Html.Should().Contain("Eproj");
        result.Html.Should().Contain("не сертифицирован");
    }

    [Fact]
    public void Generate_ReturnsDeserializableJson()
    {
        // Arrange
        var systemUnderTests = new ReportService(_options);
        var summary = CreateSummary();

        // Act
        var result = systemUnderTests.Generate(summary);
        var restored = JsonSerializer.Deserialize<AnalysisSummary>(result.Json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        restored.Should().NotBeNull();
        restored!.AoiId.Should().Be(summary.AoiId);
        restored.Units.Units.Should().Be(summary.Units.Units);
    }

    [Fact]
    public void Generate_ReturnsNonEmptyPdf()
    {
        // Arrange
        var systemUnderTests = new ReportService(_options);

        // Act
        var result = systemUnderTests.Generate(CreateSummary());

        // Assert
        result.Pdf.Should().NotBeEmpty();
    }

    [Fact]
    public void Generate_ManifestContainsMethodVersion()
    {
        // Arrange
        var systemUnderTests = new ReportService(_options);

        // Act
        var result = systemUnderTests.Generate(CreateSummary());

        // Assert
        result.ManifestJson.Should().Contain("method_version");

        using var document = JsonDocument.Parse(result.ManifestJson);
        document.RootElement.GetProperty("method_version").GetString().Should().Be(ReportService.MethodVersion);
    }

    private static AnalysisSummary CreateSummary() => new()
    {
        RunId = "test-run",
        MethodVersion = "1.0",
        DataVersion = "CCI-Biomass-v7.0",
        CreatedAt = DateTimeOffset.UtcNow,
        Status = RunStatus.Complete,
        AoiId = "AOI-TEST",
        StartYear = 2020,
        EndYear = 2021,
        PolygonAreaHectares = 100,
        YearlySeries =
        [
            new YearlyCarbonStock
            {
                Year = 2020,
                AreaHectares = 100,
                TotalCarbon = 1000,
                MeanCarbonPerHectare = 10,
                Coverage = 1,
                HasCompleteCoverage = true
            },
            new YearlyCarbonStock
            {
                Year = 2021,
                AreaHectares = 100,
                TotalCarbon = 900,
                MeanCarbonPerHectare = 9,
                Coverage = 1,
                HasCompleteCoverage = true
            }
        ],
        Change = new ProjectChange
        {
            StartYear = 2020,
            EndYear = 2021,
            DurationYears = 1,
            DeltaCarbon = -100,
            ProjectEmission = -366.67,
            EmissionPerHectarePerYear = -3.6667
        },
        Uncertainty = new UncertaintyRange
        {
            Lower = -400,
            Upper = -300,
            HalfWidth = 50,
            SensitivityCoefficient = 0.1
        },
        Baseline = new BaselineResult
        {
            HistoricalRate = -0.5,
            StartCarbonPerHectare = 10,
            EndCarbonPerHectare = 9.5,
            BaselineEmission = -183.33
        },
        Units = new UnitResult
        {
            Status = UnitStatus.Available,
            Reason = UnitBlockReason.None,
            ResultRelativeToBaseline = 183.34,
            UncertaintyDeduction = 0.1,
            AdjustedResult = 165,
            Reserve = 24.75,
            Units = 140,
            PriceScenarios =
            [
                new PriceScenario { PricePerUnit = 500, Value = 70_000 }
            ]
        },
        ChangeZones =
        [
            new ChangeZone
            {
                Id = 1,
                Geometry = new NetTopologySuite.Geometries.GeometryFactory().CreatePolygon(
                [
                    new NetTopologySuite.Geometries.Coordinate(0, 0),
                    new NetTopologySuite.Geometries.Coordinate(1, 0),
                    new NetTopologySuite.Geometries.Coordinate(1, 1),
                    new NetTopologySuite.Geometries.Coordinate(0, 1),
                    new NetTopologySuite.Geometries.Coordinate(0, 0)
                ]),
                AreaHectares = 5,
                ContributionToDeltaCarbon = -100,
                PixelCount = 1,
                Pixels =
                [
                    new ChangePixel
                    {
                        Row = 0,
                        Column = 0,
                        AreaHectares = 5,
                        BiomassChange = -20,
                        HasConfirmation = true
                    }
                ]
            }
        ],
        ChangeZoneEvidence =
        [
            new ChangeZoneEvidence
            {
                ZoneId = 1,
                EvidenceTypes = [EvidenceType.Gfc, EvidenceType.Sentinel2],
                GfcLossYears = [2021],
                EvidenceYears = [2021],
                CauseStatus = CauseStatus.Confirmed
            }
        ],
        CciChangeMeanTonnesPerHectare = 1.5,
        Warnings = ["Тестовое предупреждение"]
    };
}
