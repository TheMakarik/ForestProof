namespace ForestProof.Backend.Domain.Carbon;

/// <summary>
/// Изменение запаса углерода за выбранный период и его CO₂-эквивалент.
/// </summary>
public sealed record ProjectChange
{
    /// <summary>
    /// Начальный год периода (t₀).
    /// </summary>
    public required int StartYear { get; init; }

    /// <summary>
    /// Конечный год периода (t₁).
    /// </summary>
    public required int EndYear { get; init; }

    /// <summary>
    /// Продолжительность периода, годы (Δt = t₁ − t₀).
    /// </summary>
    public required int DurationYears { get; init; }

    /// <summary>
    /// Изменение суммарного запаса, т C (ΔC = Cₜ₁ − Cₜ₀).
    /// </summary>
    public required double DeltaCarbon { get; init; }

    /// <summary>
    /// Наблюдаемое изменение в CO₂-эквиваленте, т CO₂-экв. (Eproj).
    /// </summary>
    public required double ProjectEmission { get; init; }

    /// <summary>
    /// Изменение на гектар и один год, т CO₂-экв./га/год (e).
    /// </summary>
    public required double EmissionPerHectarePerYear { get; init; }
}
