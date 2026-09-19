namespace ForestProof.Testing.Calculation;

public sealed class ManualExampleTests
{
    [Fact]
    public void FullPipeline_WhenSpecificationExample_Reproduces395Units()
    {
        // Arrange
        var options = TestCalculationOptions.Create();
        var carbonCalculator = new CarbonCalculator(options);
        var uncertaintyCalculator = new UncertaintyCalculator(options);
        var unitCalculator = new UnitCalculator(options);

        PixelSample[] startSamples = [new() { Biomass = 100, AreaHectares = 100, StandardDeviation = 0.3 }];
        PixelSample[] endSamples = [new() { Biomass = 104, AreaHectares = 100, StandardDeviation = 0.3 }];

        var start = carbonCalculator.AggregateCarbonStock(startSamples);
        var end = carbonCalculator.AggregateCarbonStock(endSamples);
        var change = carbonCalculator.CalculateChange(start, end, 2023, 2024);
        var range = uncertaintyCalculator.Calculate(startSamples, endSamples, change.ProjectEmission, 1);

        // Act
        var units = unitCalculator.Calculate(new UnitCalculationInput
        {
            AreaHectares = end.AreaHectares,
            StartYear = 2023,
            EndYear = 2024,
            HasMandatoryData = true,
            HasCompleteCoverage = true,
            HasBaseline = true,
            BaselineEmission = -172.3333333333,
            ProjectEmission = change.ProjectEmission,
            Uncertainty = range.HalfWidth,
            Leakage = 0
        });

        // Assert
        change.ProjectEmission.Should().BeApproximately(-689.3333333333, 1e-6);
        range.HalfWidth.Should().BeApproximately(103.4, 1e-6);
        units.Units.Should().Be(395);
    }
}
