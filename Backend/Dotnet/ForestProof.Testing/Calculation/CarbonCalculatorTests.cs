namespace ForestProof.Testing.Calculation;

public sealed class CarbonCalculatorTests
{
    private readonly IOptions<CalculationOptions> _options;

    public CarbonCalculatorTests()
    {
        _options = TestCalculationOptions.Create();
    }

    [Fact]
    public void AggregateCarbonStock_WhenManualExample_ReturnsExpectedStock()
    {
        // Arrange
        var systemUnderTests = new CarbonCalculator(_options);
        PixelSample[] samples =
        [
            new() { Biomass = 100, AreaHectares = 100 }
        ];

        // Act
        var stock = systemUnderTests.AggregateCarbonStock(samples);

        // Assert
        stock.AreaHectares.Should().Be(100);
        stock.TotalCarbon.Should().BeApproximately(4700, 1e-9);
        stock.MeanCarbonPerHectare.Should().BeApproximately(47, 1e-9);
    }

    [Fact]
    public void AggregateCarbonStock_WhenZeroBiomass_KeepsSampleAsValid()
    {
        // Arrange
        var systemUnderTests = new CarbonCalculator(_options);
        PixelSample[] samples =
        [
            new() { Biomass = 0, AreaHectares = 10 },
            new() { Biomass = 100, AreaHectares = 10 }
        ];

        // Act
        var stock = systemUnderTests.AggregateCarbonStock(samples);

        // Assert
        stock.AreaHectares.Should().Be(20);
        stock.TotalCarbon.Should().BeApproximately(100 * 0.47 * 10, 1e-9);
    }

    [Fact]
    public void AggregateCarbonStock_WhenPartialPixelAreas_WeightsByArea()
    {
        // Arrange
        var systemUnderTests = new CarbonCalculator(_options);
        PixelSample[] samples =
        [
            new() { Biomass = 100, AreaHectares = 0.4 },
            new() { Biomass = 100, AreaHectares = 0.8 },
            new() { Biomass = 100, AreaHectares = 1.0 }
        ];

        // Act
        var stock = systemUnderTests.AggregateCarbonStock(samples);

        // Assert
        stock.AreaHectares.Should().BeApproximately(2.2, 1e-9);
        stock.TotalCarbon.Should().BeApproximately(100 * 0.47 * 2.2, 1e-9);
    }

    [Fact]
    public void CalculateChange_WhenManualExample_ReturnsExpectedEmission()
    {
        // Arrange
        var systemUnderTests = new CarbonCalculator(_options);
        var start = new CarbonStock { AreaHectares = 100, TotalCarbon = 4700, MeanCarbonPerHectare = 47 };
        var end = new CarbonStock { AreaHectares = 100, TotalCarbon = 4888, MeanCarbonPerHectare = 48.88 };

        // Act
        var change = systemUnderTests.CalculateChange(start, end, 2023, 2024);

        // Assert
        change.DurationYears.Should().Be(1);
        change.DeltaCarbon.Should().BeApproximately(188, 1e-9);
        change.ProjectEmission.Should().BeApproximately(-188 * 44.0 / 12.0, 1e-6);
        change.EmissionPerHectarePerYear.Should().BeApproximately(-188 * 44.0 / 12.0 / 100, 1e-6);
    }

    [Fact]
    public void CalculateChange_WhenPeriodNotPositive_Throws()
    {
        // Arrange
        var systemUnderTests = new CarbonCalculator(_options);
        var stock = new CarbonStock { AreaHectares = 100, TotalCarbon = 4700, MeanCarbonPerHectare = 47 };
        Action act = () => systemUnderTests.CalculateChange(stock, stock, 2024, 2024);

        // Act
        var exception = act.Should().Throw<ArgumentOutOfRangeException>();

        // Assert
        exception.Which.Message.Should().Contain("больше начального");
    }
}
