namespace ForestProof.Testing.Calculation;

public sealed class BaselineCalculatorTests
{
    private readonly IOptions<CalculationOptions> _options;

    public BaselineCalculatorTests()
    {
        _options = TestCalculationOptions.Create();
    }

    [Fact]
    public void Calculate_WhenRisingBaseline_ProjectsHistoricalRate()
    {
        // Arrange
        var systemUnderTests = new BaselineCalculator(_options);

        // Act
        var result = systemUnderTests.Calculate(
            historicalCarbonPerHectare: 45,
            referenceCarbonPerHectare: 47,
            areaHectares: 100,
            startYear: 2019,
            endYear: 2024);

        // Assert
        result.HistoricalRate.Should().BeApproximately(0.5, 1e-9);
        result.StartCarbonPerHectare.Should().BeApproximately(47, 1e-9);
        result.EndCarbonPerHectare.Should().BeApproximately(49.5, 1e-9);
        result.BaselineEmission.Should().BeApproximately(-100 * 2.5 * 44.0 / 12.0, 1e-6);
    }

    [Fact]
    public void Calculate_WhenProjectionNegative_ClampsToZero()
    {
        // Arrange
        var systemUnderTests = new BaselineCalculator(_options);

        // Act
        var result = systemUnderTests.Calculate(
            historicalCarbonPerHectare: 100,
            referenceCarbonPerHectare: 0,
            areaHectares: 10,
            startYear: 2019,
            endYear: 2024);

        // Assert
        result.EndCarbonPerHectare.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenRisingBaseline_ReturnsNegativeEmission()
    {
        // Arrange
        var systemUnderTests = new BaselineCalculator(_options);

        // Act
        var result = systemUnderTests.Calculate(45, 47, 100, 2019, 2024);

        // Assert
        result.BaselineEmission.Should().BeNegative();
    }

    [Fact]
    public void Calculate_WhenHistoricalPeriodNotPositive_Throws()
    {
        // Arrange
        var options = TestCalculationOptions.Create(
            baselineHistoricalYear: 2019,
            baselineReferenceYear: 2019);
        var systemUnderTests = new BaselineCalculator(options);

        // Act
        Action act = () => systemUnderTests.Calculate(45, 47, 100, 2019, 2024);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Calculate_WhenReferenceCarbonNotFinite_Throws()
    {
        // Arrange
        var systemUnderTests = new BaselineCalculator(_options);

        // Act
        Action act = () => systemUnderTests.Calculate(45, double.NaN, 100, 2019, 2024);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
