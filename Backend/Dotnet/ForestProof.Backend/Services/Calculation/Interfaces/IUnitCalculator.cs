using ForestProof.Backend.Domain.Units;

namespace ForestProof.Backend.Services.Calculation.Interfaces;

/// <summary>
/// Считает потенциальные единицы Q и разделяет нулевой и недоступный результаты.
/// </summary>
public interface IUnitCalculator
{
    /// <summary>
    /// Рассчитывает результат относительно baseline и потенциальные единицы.
    /// </summary>
    /// <param name="input">Результаты Ebase/Eproj, неопределённость и условия допустимости.</param>
    /// <returns>Статус, причину, R, UNC, Radj, B, Q и сценарные стоимости.</returns>
    UnitResult Calculate(UnitCalculationInput input);
}
