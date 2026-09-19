using ForestProof.Backend.Domain.Carbon;
using ForestProof.Backend.Domain.Pixels;

namespace ForestProof.Backend.Services.Calculation.Interfaces;

/// <summary>
/// Считает запас углерода и его изменение по пиксельным данным биомассы.
/// </summary>
public interface ICarbonCalculator
{
    /// <summary>
    /// Агрегирует запас углерода по валидным пикселям с учётом площади пересечения.
    /// </summary>
    /// <param name="samples">Валидные пиксели с биомассой и площадью пересечения, га.</param>
    /// <returns>Расчётную площадь, суммарный и удельный запас углерода.</returns>
    CarbonStock AggregateCarbonStock(IReadOnlyCollection<PixelSample> samples);

    /// <summary>
    /// Считает изменение запаса и CO₂-эквивалент между двумя годами.
    /// </summary>
    /// <param name="start">Запас на начальную дату.</param>
    /// <param name="end">Запас на конечную дату.</param>
    /// <param name="startYear">Начальный год (t₀).</param>
    /// <param name="endYear">Конечный год (t₁).</param>
    /// <returns>ΔC, Eproj, e и продолжительность периода.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Если конечный год не больше начального.</exception>
    ProjectChange CalculateChange(CarbonStock start, CarbonStock end, int startYear, int endYear);
}
