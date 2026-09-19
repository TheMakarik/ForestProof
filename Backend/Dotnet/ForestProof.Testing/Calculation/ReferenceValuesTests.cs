namespace ForestProof.Testing.Calculation;

public sealed class ReferenceValuesTests
{
    [Fact]
    public void CarbonChain_FromSpecification_MatchesReferenceValues()
    {
        // Arrange: A = 100 га, биомасса 100 -> 104 т/га
        var options = TestCalculationOptions.Create();
        var carbonCalculator = new CarbonCalculator(options);
        PixelSample[] start = [new() { Biomass = 100, AreaHectares = 100 }];
        PixelSample[] end = [new() { Biomass = 104, AreaHectares = 100 }];

        // Act
        var startStock = carbonCalculator.AggregateCarbonStock(start);
        var endStock = carbonCalculator.AggregateCarbonStock(end);
        var change = carbonCalculator.CalculateChange(startStock, endStock, 2023, 2024);

        // Assert
        startStock.MeanCarbonPerHectare.Should().BeApproximately(47, 1e-6);
        endStock.MeanCarbonPerHectare.Should().BeApproximately(48.88, 1e-6);
        change.DeltaCarbon.Should().BeApproximately(188, 1e-6);
        change.ProjectEmission.Should().BeApproximately(-689.333, 1e-3);
        change.EmissionPerHectarePerYear.Should().BeApproximately(-6.893, 1e-3);
    }

    [Fact]
    public void UnitsChain_FromSpecification_MatchesReferenceValues()
    {
        // Arrange: Ebase = -172.333, Eproj = -689.333, H = 103.4
        var options = TestCalculationOptions.Create();
        var unitCalculator = new UnitCalculator(options);

        // Act
        var units = unitCalculator.Calculate(new UnitCalculationInput
        {
            AreaHectares = 100,
            StartYear = 2023,
            EndYear = 2024,
            HasMandatoryData = true,
            HasCompleteCoverage = true,
            HasBaseline = true,
            BaselineEmission = -172.333,
            ProjectEmission = -689.333,
            Uncertainty = 103.4,
            Leakage = 0
        });

        // Assert
        units.ResultRelativeToBaseline.Should().BeApproximately(517, 1e-9);
        units.UncertaintyDeduction.Should().BeApproximately(0.10, 1e-9);
        units.AdjustedResult.Should().BeApproximately(465.3, 1e-9);
        units.Reserve.Should().BeApproximately(69.795, 1e-9);
        units.Units.Should().Be(395);
    }
}
