using ForestProof.Backend.Domain.Enums;
using ForestProof.Backend.Domain.Pricing;
using ForestProof.Backend.Domain.Units;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Calculation.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Calculation;

/// <summary>
/// Считает потенциальные единицы Q и разделяет нулевой и недоступный результаты.
/// </summary>
/// <param name="options">Параметры расчёта (порог UNC, резерв и цены).</param>
public sealed class UnitCalculator(IOptions<CalculationOptions> options) : IUnitCalculator
{
    private readonly CalculationOptions _options = options.Value;

    /// <inheritdoc />
    public UnitResult Calculate(UnitCalculationInput input)
    {
        if (input.AreaHectares <= 0)
            return Unavailable(UnitBlockReason.ZeroArea);

        if (input.EndYear - input.StartYear <= 0)
            return Unavailable(UnitBlockReason.NonPositivePeriod);

        if (!input.HasMandatoryData)
            return Unavailable(UnitBlockReason.MissingData);

        if (!input.HasCompleteCoverage)
            return Unavailable(UnitBlockReason.IncompleteCoverage);

        if (!input.HasBaseline)
            return Unavailable(UnitBlockReason.MissingBaseline);

        if (!double.IsFinite(input.BaselineEmission) ||
            !double.IsFinite(input.ProjectEmission) ||
            !double.IsFinite(input.Uncertainty) ||
            !double.IsFinite(input.Leakage))
            throw new ArgumentException(
                "Входные значения расчёта единиц должны быть конечными числами.",
                nameof(input));

        var result = input.BaselineEmission - input.ProjectEmission - input.Leakage;
        if (!double.IsFinite(result))
            throw new ArgumentException(
                "Результат относительно baseline должен быть конечным числом.",
                nameof(input));

        if (result <= 0)
            return Zero(result, UnitBlockReason.NonPositiveResult);

        var uncertaintyRatio = input.Uncertainty / result;
        if (uncertaintyRatio >= 1)
            return Zero(result, UnitBlockReason.UncertaintyExceedsResult);

        var deduction = Math.Min(1, Math.Max(0, uncertaintyRatio - _options.UncertaintyThreshold));
        var adjustedResult = result * (1 - deduction);
        var reserve = adjustedResult * _options.ReserveFraction;
        var units = (int)Math.Floor(adjustedResult * (1 - _options.ReserveFraction));

        return new UnitResult
        {
            Status = UnitStatus.Available,
            Reason = UnitBlockReason.None,
            ResultRelativeToBaseline = result,
            UncertaintyDeduction = deduction,
            AdjustedResult = adjustedResult,
            Reserve = reserve,
            Units = units,
            PriceScenarios = BuildPriceScenarios(units)
        };
    }

    private UnitResult Unavailable(UnitBlockReason reason) => new()
    {
        Status = UnitStatus.Unavailable,
        Reason = reason,
        ResultRelativeToBaseline = 0,
        UncertaintyDeduction = 0,
        AdjustedResult = 0,
        Reserve = 0,
        Units = null,
        PriceScenarios = []
    };

    private UnitResult Zero(double result, UnitBlockReason reason) => new()
    {
        Status = UnitStatus.Zero,
        Reason = reason,
        ResultRelativeToBaseline = result,
        UncertaintyDeduction = 0,
        AdjustedResult = 0,
        Reserve = 0,
        Units = 0,
        PriceScenarios = BuildPriceScenarios(0)
    };

    private IReadOnlyList<PriceScenario> BuildPriceScenarios(int units) =>
        _options.PriceScenariosRubles
            .Select(price => new PriceScenario
            {
                PricePerUnit = price,
                Value = (double)units * price
            })
            .ToArray();
}
