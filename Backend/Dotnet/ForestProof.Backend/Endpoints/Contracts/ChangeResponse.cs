namespace ForestProof.Backend.Endpoints.Contracts;

/// <summary>
/// Изменение запаса углерода за период и его CO₂-эквивалент.
/// </summary>
public sealed record ChangeResponse
{
    /// <summary>
    /// Изменение суммарного запаса, т C (ΔC).
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
