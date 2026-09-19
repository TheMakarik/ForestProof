using ForestProof.Backend.Domain.Carbon;
using ForestProof.Backend.Domain.Pixels;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Calculation.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Calculation;

/// <summary>
/// Считает запас углерода и его изменение по пиксельным данным биомассы.
/// </summary>
/// <param name="options">Параметры расчёта (коэффициент CF и отношение CO₂/C).</param>
public sealed class CarbonCalculator(IOptions<CalculationOptions> options) : ICarbonCalculator
{
    private readonly CalculationOptions _options = options.Value;

    /// <inheritdoc />
    public CarbonStock AggregateCarbonStock(IReadOnlyCollection<PixelSample> samples)
    {
        var areaHectares = 0.0;
        var totalCarbon = 0.0;

        foreach (var sample in samples)
        {
            areaHectares += sample.AreaHectares;
            totalCarbon += sample.Biomass * _options.CarbonFraction * sample.AreaHectares;
        }

        var meanCarbonPerHectare = areaHectares > 0
            ? totalCarbon / areaHectares
            : 0;

        return new CarbonStock
        {
            AreaHectares = areaHectares,
            TotalCarbon = totalCarbon,
            MeanCarbonPerHectare = meanCarbonPerHectare
        };
    }

    /// <inheritdoc />
    public ProjectChange CalculateChange(CarbonStock start, CarbonStock end, int startYear, int endYear)
    {
        var durationYears = endYear - startYear;
        if (durationYears <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(endYear),
                endYear,
                "Конечный год должен быть больше начального.");

        var deltaCarbon = end.TotalCarbon - start.TotalCarbon;
        var projectEmission = -deltaCarbon * _options.Co2PerCarbonRatio;
        var emissionPerHectarePerYear = end.AreaHectares > 0
            ? projectEmission / (end.AreaHectares * durationYears)
            : 0;

        return new ProjectChange
        {
            StartYear = startYear,
            EndYear = endYear,
            DurationYears = durationYears,
            DeltaCarbon = deltaCarbon,
            ProjectEmission = projectEmission,
            EmissionPerHectarePerYear = emissionPerHectarePerYear
        };
    }
}
