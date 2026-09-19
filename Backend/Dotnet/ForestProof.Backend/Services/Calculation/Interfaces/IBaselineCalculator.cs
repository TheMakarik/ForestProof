using ForestProof.Backend.Domain.Baseline;

namespace ForestProof.Backend.Services.Calculation.Interfaces;

/// <summary>
/// Считает базовую линию: исторический темп, сценарные запасы и Ebase.
/// </summary>
public interface IBaselineCalculator
{
    /// <summary>
    /// Проецирует исторический темп базовой линии на годы запроса.
    /// </summary>
    /// <param name="historicalCarbonPerHectare">Сценарный запас в историческом году, т C/га.</param>
    /// <param name="referenceCarbonPerHectare">Сценарный запас в опорном году, т C/га.</param>
    /// <param name="areaHectares">Расчётная площадь, га (A).</param>
    /// <param name="startYear">Начальный год периода (t₀).</param>
    /// <param name="endYear">Конечный год периода (t₁).</param>
    /// <returns>Исторический темп g, сценарные запасы на границах и Ebase.</returns>
    BaselineResult Calculate(
        double historicalCarbonPerHectare,
        double referenceCarbonPerHectare,
        double areaHectares,
        int startYear,
        int endYear);
}
