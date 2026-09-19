namespace ForestProof.Backend.Domain.Units;

/// <summary>
/// Вход для расчёта потенциальных единиц: результат относительно baseline и условия допустимости.
/// </summary>
public sealed record UnitCalculationInput
{
    /// <summary>
    /// Расчётная площадь, га (A).
    /// </summary>
    public required double AreaHectares { get; init; }

    /// <summary>
    /// Начальный год периода (t₀).
    /// </summary>
    public required int StartYear { get; init; }

    /// <summary>
    /// Конечный год периода (t₁).
    /// </summary>
    public required int EndYear { get; init; }

    /// <summary>
    /// Признак наличия всех обязательных данных.
    /// </summary>
    public required bool HasMandatoryData { get; init; }

    /// <summary>
    /// Признак полного обязательного покрытия обеих дат.
    /// </summary>
    public required bool HasCompleteCoverage { get; init; }

    /// <summary>
    /// Признак наличия базовой линии для территории.
    /// </summary>
    public required bool HasBaseline { get; init; }

    /// <summary>
    /// Изменение по базовой линии, т CO₂-экв. (Ebase).
    /// </summary>
    public required double BaselineEmission { get; init; }

    /// <summary>
    /// Наблюдаемое изменение, т CO₂-экв. (Eproj).
    /// </summary>
    public required double ProjectEmission { get; init; }

    /// <summary>
    /// Ширина сценарного диапазона, т CO₂-экв. (H).
    /// </summary>
    public required double Uncertainty { get; init; }

    /// <summary>
    /// Вычет за утечку, т CO₂-экв. (LK); в кейсе 0.
    /// </summary>
    public required double Leakage { get; init; }
}
