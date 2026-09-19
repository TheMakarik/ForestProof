namespace ForestProof.Testing.Calculation;

public sealed class UnitCalculatorTests
{
    private readonly IOptions<CalculationOptions> _options;

    public UnitCalculatorTests()
    {
        _options = TestCalculationOptions.Create();
    }

    [Fact]
    public void Calculate_WhenManualExample_Returns395Units()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);

        // Act
        var result = systemUnderTests.Calculate(CreateManualExampleInput());

        // Assert
        result.Status.Should().Be(UnitStatus.Available);
        result.Reason.Should().Be(UnitBlockReason.None);
        result.ResultRelativeToBaseline.Should().BeApproximately(517, 1e-6);
        result.UncertaintyDeduction.Should().BeApproximately(0.10, 1e-9);
        result.AdjustedResult.Should().BeApproximately(465.3, 1e-6);
        result.Reserve.Should().BeApproximately(69.795, 1e-6);
        result.Units.Should().Be(395);
    }

    [Fact]
    public void Calculate_WhenManualExample_ReturnsPriceScenarios()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);

        // Act
        var result = systemUnderTests.Calculate(CreateManualExampleInput());

        // Assert
        result.PriceScenarios.Select(scenario => scenario.Value)
            .Should().Equal(395 * 500.0, 395 * 1500.0, 395 * 4000.0);
    }

    [Fact]
    public void Calculate_WhenResultNotPositive_ReturnsZeroUnits()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);
        var input = CreateManualExampleInput() with { BaselineEmission = 0, ProjectEmission = 100 };

        // Act
        var result = systemUnderTests.Calculate(input);

        // Assert
        result.Status.Should().Be(UnitStatus.Zero);
        result.Reason.Should().Be(UnitBlockReason.NonPositiveResult);
        result.Units.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenUncertaintyExceedsResult_ReturnsZeroUnits()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);
        var input = CreateManualExampleInput() with { Uncertainty = 517 };

        // Act
        var result = systemUnderTests.Calculate(input);

        // Assert
        result.Status.Should().Be(UnitStatus.Zero);
        result.Reason.Should().Be(UnitBlockReason.UncertaintyExceedsResult);
        result.Units.Should().Be(0);
    }

    [Fact]
    public void Calculate_WhenCoverageIncomplete_ReturnsUnavailable()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);
        var input = CreateManualExampleInput() with { HasCompleteCoverage = false };

        // Act
        var result = systemUnderTests.Calculate(input);

        // Assert
        result.Status.Should().Be(UnitStatus.Unavailable);
        result.Reason.Should().Be(UnitBlockReason.IncompleteCoverage);
        result.Units.Should().BeNull();
    }

    [Fact]
    public void Calculate_WhenAreaZero_ReturnsUnavailable()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);
        var input = CreateManualExampleInput() with { AreaHectares = 0 };

        // Act
        var result = systemUnderTests.Calculate(input);

        // Assert
        result.Status.Should().Be(UnitStatus.Unavailable);
        result.Reason.Should().Be(UnitBlockReason.ZeroArea);
        result.Units.Should().BeNull();
    }

    [Fact]
    public void Calculate_WhenBaselineMissing_ReturnsUnavailable()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);
        var input = CreateManualExampleInput() with { HasBaseline = false };

        // Act
        var result = systemUnderTests.Calculate(input);

        // Assert
        result.Reason.Should().Be(UnitBlockReason.MissingBaseline);
        result.Units.Should().BeNull();
    }

    [Fact]
    public void Calculate_WhenUncertaintyBelowThreshold_DoesNotDeduct()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);
        var input = CreateManualExampleInput() with { Uncertainty = 51.7 };

        // Act
        var result = systemUnderTests.Calculate(input);

        // Assert
        result.UncertaintyDeduction.Should().Be(0);
        result.AdjustedResult.Should().BeApproximately(517, 1e-6);
        result.Units.Should().Be(439);
    }

    [Fact]
    public void Calculate_WhenUncertaintyNotFinite_Throws()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);
        var input = CreateManualExampleInput() with { Uncertainty = double.NaN };

        // Act
        Action act = () => systemUnderTests.Calculate(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Calculate_WhenResultNotFinite_Throws()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);
        var input = CreateManualExampleInput() with { BaselineEmission = double.NaN };

        // Act
        Action act = () => systemUnderTests.Calculate(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Calculate_WhenUnitsTimesPriceExceedsIntRange_ReturnsWidenedValue()
    {
        // Arrange
        var systemUnderTests = new UnitCalculator(_options);
        var input = CreateManualExampleInput() with
        {
            BaselineEmission = 2_350_000_000,
            ProjectEmission = 0,
            Uncertainty = 0
        };

        // Act
        var result = systemUnderTests.Calculate(input);

        // Assert
        var expected = result.Units!.Value * 4000.0;
        result.PriceScenarios[^1].Value.Should().BeApproximately(expected, 1.0);
    }

    private static UnitCalculationInput CreateManualExampleInput() => new()
    {
        AreaHectares = 100,
        StartYear = 2023,
        EndYear = 2024,
        HasMandatoryData = true,
        HasCompleteCoverage = true,
        HasBaseline = true,
        BaselineEmission = -172.3333333333,
        ProjectEmission = -689.3333333333,
        Uncertainty = 103.4,
        Leakage = 0
    };
}
