using ForestProof.Backend.Domain.Baseline;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Calculation.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Calculation;

/// <summary>
/// Считает базовую линию: исторический темп, сценарные запасы и Ebase.
/// </summary>
/// <param name="options">Параметры расчёта (опорные годы и отношение CO₂/C).</param>
public sealed class BaselineCalculator(IOptions<CalculationOptions> options) : IBaselineCalculator
{
    private readonly CalculationOptions _options = options.Value;

    /// <inheritdoc />
    public BaselineResult Calculate(
        double historicalCarbonPerHectare,
        double referenceCarbonPerHectare,
        double areaHectares,
        int startYear,
        int endYear)
    {
        var historicalPeriod = _options.BaselineReferenceYear - _options.BaselineHistoricalYear;
        if (historicalPeriod <= 0)
            throw new ArgumentException("Исторический период базовой линии должен быть положительным.");

        if (!double.IsFinite(historicalCarbonPerHectare) || !double.IsFinite(referenceCarbonPerHectare))
            throw new ArgumentException(
                "Значения базовой линии должны быть конечными числами.",
                nameof(referenceCarbonPerHectare));

        var historicalRate = (referenceCarbonPerHectare - historicalCarbonPerHectare) / historicalPeriod;

        var startCarbonPerHectare = Project(referenceCarbonPerHectare, historicalRate, startYear);
        var endCarbonPerHectare = Project(referenceCarbonPerHectare, historicalRate, endYear);

        var baselineEmission =
            -areaHectares * (endCarbonPerHectare - startCarbonPerHectare) * _options.Co2PerCarbonRatio;

        return new BaselineResult
        {
            HistoricalRate = historicalRate,
            StartCarbonPerHectare = startCarbonPerHectare,
            EndCarbonPerHectare = endCarbonPerHectare,
            BaselineEmission = baselineEmission
        };
    }

    private double Project(double referenceCarbonPerHectare, double historicalRate, int year) =>
        Math.Max(0, referenceCarbonPerHectare + historicalRate * (year - _options.BaselineReferenceYear));
}
