using System.ComponentModel.DataAnnotations;

namespace ForestProof.Backend.Options;

/// <summary>
/// Параметры методики расчёта: коэффициенты, пороги, границы и сценарии.
/// </summary>
public sealed class CalculationOptions
{
    /// <summary>
    /// Доля углерода в сухом веществе, т C/т сухого вещества (CF).
    /// </summary>
    [Range(0.0, 1.0)]
    public required double CarbonFraction { get; init; }

    /// <summary>
    /// Отношение молярной массы CO₂ к углероду (44/12).
    /// </summary>
    [Range(1.0, 10.0)]
    public required double Co2PerCarbonRatio { get; init; }

    /// <summary>
    /// Разрешённый порог неопределённости, который не вычитается (доля).
    /// </summary>
    [Range(0.0, 1.0)]
    public required double UncertaintyThreshold { get; init; }

    /// <summary>
    /// Доля резерва от скорректированного результата.
    /// </summary>
    [Range(0.0, 1.0)]
    public required double ReserveFraction { get; init; }

    /// <summary>
    /// Коэффициент чувствительности k по умолчанию.
    /// </summary>
    [Range(1.0, 10.0)]
    public required double DefaultSensitivityCoefficient { get; init; }

    /// <summary>
    /// Максимальная допустимая площадь полигона, км².
    /// </summary>
    [Range(0.0, double.MaxValue)]
    public required double MaxAreaSquareKilometers { get; init; }

    /// <summary>
    /// Минимально допустимый год анализа.
    /// </summary>
    [Range(1900, 2100)]
    public required int MinAnalysisYear { get; init; }

    /// <summary>
    /// Максимально допустимый год анализа.
    /// </summary>
    [Range(1900, 2100)]
    public required int MaxAnalysisYear { get; init; }

    /// <summary>
    /// Вычет за утечку, т CO₂-экв. (LK); в кейсе 0.
    /// </summary>
    [Range(0.0, double.MaxValue)]
    public required double LeakageTonnesCo2 { get; init; }

    /// <summary>
    /// Порог чувствительности по модулю изменения AGB, т/га.
    /// </summary>
    [Range(0.0, double.MaxValue)]
    public required double ChangeDetectionThresholdTonnesPerHectare { get; init; }

    /// <summary>
    /// Минимальная площадь зоны изменений, га.
    /// </summary>
    [Range(0.0, double.MaxValue)]
    public required double MinChangeZoneAreaHectares { get; init; }

    /// <summary>
    /// Порог dNBR для подтверждения изменения Sentinel-2.
    /// </summary>
    [Range(0.0, 2.0)]
    public required double SentinelDnbrThreshold { get; init; }

    /// <summary>
    /// Версия методики расчёта.
    /// </summary>
    [Required]
    public required string MethodVersion { get; init; }

    /// <summary>
    /// Версия используемого набора данных.
    /// </summary>
    [Required]
    public required string DataVersion { get; init; }

    /// <summary>
    /// Исторический год базовой линии.
    /// </summary>
    [Range(1900, 2100)]
    public required int BaselineHistoricalYear { get; init; }

    /// <summary>
    /// Опорный год базовой линии.
    /// </summary>
    [Range(1900, 2100)]
    public required int BaselineReferenceYear { get; init; }

    /// <summary>
    /// SCL-классы, допустимые для растительного анализа.
    /// </summary>
    [MinLength(1)]
    public required IReadOnlyList<int> AllowedSclClasses { get; init; }

    /// <summary>
    /// Расширенные SCL-классы для исследовательского режима.
    /// </summary>
    [MinLength(1)]
    public required IReadOnlyList<int> ExtendedSclClasses { get; init; }

    /// <summary>
    /// Сценарные цены одной единицы, руб.
    /// </summary>
    [MinLength(1)]
    public required IReadOnlyList<int> PriceScenariosRubles { get; init; }
}
