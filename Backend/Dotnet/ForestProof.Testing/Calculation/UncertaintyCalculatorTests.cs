namespace ForestProof.Testing.Calculation;

public sealed class UncertaintyCalculatorTests
{
    private readonly IOptions<CalculationOptions> _options;

    public UncertaintyCalculatorTests()
    {
        _options = TestCalculationOptions.Create();
    }

    [Fact]
    public void Calculate_WhenManualExample_ReturnsExpectedRange()
    {
        // Arrange
        var systemUnderTests = new UncertaintyCalculator(_options);
        PixelSample[] start = [new() { Biomass = 100, AreaHectares = 100, StandardDeviation = 0.3 }];
        PixelSample[] end = [new() { Biomass = 104, AreaHectares = 100, StandardDeviation = 0.3 }];
        var projectEmission = -188 * 44.0 / 12.0;
        var expectedLower = (99.7 * 0.47 * 100 - 104.3 * 0.47 * 100) * 44.0 / 12.0;
        var expectedUpper = (100.3 * 0.47 * 100 - 103.7 * 0.47 * 100) * 44.0 / 12.0;

        // Act
        var range = systemUnderTests.Calculate(start, end, projectEmission, 1);

        // Assert
        range.Lower.Should().BeApproximately(expectedLower, 1e-6);
        range.Upper.Should().BeApproximately(expectedUpper, 1e-6);
        range.HalfWidth.Should().BeApproximately(103.4, 1e-6);
        range.Lower.Should().BeLessThanOrEqualTo(projectEmission);
        range.Upper.Should().BeGreaterThanOrEqualTo(projectEmission);
    }

    [Fact]
    public void Calculate_WhenLowerBoundNegative_ClampsToZero()
    {
        // Arrange
        var systemUnderTests = new UncertaintyCalculator(_options);
        PixelSample[] start = [new() { Biomass = 0.2, AreaHectares = 100, StandardDeviation = 5 }];
        PixelSample[] end = [new() { Biomass = 0.2, AreaHectares = 100, StandardDeviation = 5 }];
        var high = 5.2 * 0.47 * 100;

        // Act
        var range = systemUnderTests.Calculate(start, end, 0, 1);

        // Assert
        range.Lower.Should().BeApproximately((0 - high) * 44.0 / 12.0, 1e-6);
        range.Upper.Should().BeApproximately((high - 0) * 44.0 / 12.0, 1e-6);
    }

    [Fact]
    public void Calculate_WhenSampleHasNoStandardDeviation_ExcludesSample()
    {
        // Arrange
        var systemUnderTests = new UncertaintyCalculator(_options);
        PixelSample[] start = [new() { Biomass = 100, AreaHectares = 100 }];
        PixelSample[] end = [new() { Biomass = 100, AreaHectares = 100 }];

        // Act
        var range = systemUnderTests.Calculate(start, end, 0, 1);

        // Assert
        range.HalfWidth.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenSomeSamplesHaveNoStandardDeviation_KeepsProjectEmissionWithinRange()
    {
        // Arrange
        var systemUnderTests = new UncertaintyCalculator(_options);
        PixelSample[] start = [new() { Biomass = 100, AreaHectares = 100, StandardDeviation = 0.3 }];
        PixelSample[] end = [new() { Biomass = 100, AreaHectares = 100 }];
        var projectEmission = 0.0;

        // Act
        var range = systemUnderTests.Calculate(start, end, projectEmission, 1);

        // Assert
        range.Lower.Should().BeLessThanOrEqualTo(projectEmission);
        range.Upper.Should().BeGreaterThanOrEqualTo(projectEmission);
    }
}
