namespace ForestProof.Testing.Infrastructure;

public static class TestCalculationOptions
{
    public static IOptions<CalculationOptions> Create(
        int baselineHistoricalYear = 2015,
        int baselineReferenceYear = 2019)
    {
        var options = A.Fake<IOptions<CalculationOptions>>();
        A.CallTo(() => options.Value).Returns(new CalculationOptions
        {
            CarbonFraction = 0.47,
            Co2PerCarbonRatio = 44.0 / 12.0,
            UncertaintyThreshold = 0.10,
            ReserveFraction = 0.15,
            DefaultSensitivityCoefficient = 1,
            MaxAreaSquareKilometers = 20,
            MinAnalysisYear = 2019,
            MaxAnalysisYear = 2024,
            LeakageTonnesCo2 = 0,
            ChangeDetectionThresholdTonnesPerHectare = 10,
            MinChangeZoneAreaHectares = 1,
            SentinelDnbrThreshold = 0.1,
            MethodVersion = "1.0",
            DataVersion = "CCI-Biomass-v7.0",
            BaselineHistoricalYear = baselineHistoricalYear,
            BaselineReferenceYear = baselineReferenceYear,
            AllowedSclClasses = [4, 5],
            ExtendedSclClasses = [6, 7],
            PriceScenariosRubles = [500, 1500, 4000]
        });

        return options;
    }
}
