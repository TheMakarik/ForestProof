using ForestProof.Backend.Domain.Pixels;
using ForestProof.Backend.Domain.Uncertainty;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Calculation.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Calculation;

/// <summary>
/// Считает сценарный диапазон L–U и ширину H по стандартному отклонению биомассы.
/// </summary>
/// <param name="options">Параметры расчёта (коэффициент CF и отношение CO₂/C).</param>
public sealed class UncertaintyCalculator(IOptions<CalculationOptions> options) : IUncertaintyCalculator
{
    private readonly CalculationOptions _options = options.Value;

    /// <inheritdoc />
    public UncertaintyRange Calculate(
        IReadOnlyCollection<PixelSample> startSamples,
        IReadOnlyCollection<PixelSample> endSamples,
        double projectEmission,
        double sensitivityCoefficient)
    {
        var startLow = AggregateBound(startSamples, sensitivityCoefficient, upper: false);
        var startHigh = AggregateBound(startSamples, sensitivityCoefficient, upper: true);
        var endLow = AggregateBound(endSamples, sensitivityCoefficient, upper: false);
        var endHigh = AggregateBound(endSamples, sensitivityCoefficient, upper: true);

        double[] scenarios =
        [
            (startLow - endLow) * _options.Co2PerCarbonRatio,
            (startLow - endHigh) * _options.Co2PerCarbonRatio,
            (startHigh - endLow) * _options.Co2PerCarbonRatio,
            (startHigh - endHigh) * _options.Co2PerCarbonRatio
        ];

        var lower = scenarios.Min();
        var upper = scenarios.Max();
        var halfWidth = Math.Max(projectEmission - lower, upper - projectEmission);

        return new UncertaintyRange
        {
            Lower = lower,
            Upper = upper,
            HalfWidth = halfWidth,
            SensitivityCoefficient = sensitivityCoefficient
        };
    }

    private double AggregateBound(
        IReadOnlyCollection<PixelSample> samples,
        double sensitivityCoefficient,
        bool upper)
    {
        var totalCarbon = 0.0;

        foreach (var sample in samples)
        {
            var standardDeviation = sample.StandardDeviation ?? 0;

            var biomass = upper
                ? sample.Biomass + sensitivityCoefficient * standardDeviation
                : Math.Max(0, sample.Biomass - sensitivityCoefficient * standardDeviation);

            totalCarbon += biomass * _options.CarbonFraction * sample.AreaHectares;
        }

        return totalCarbon;
    }
}
