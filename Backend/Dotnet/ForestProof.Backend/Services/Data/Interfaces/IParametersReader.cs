using ForestProof.Backend.Domain.Methodology;

namespace ForestProof.Backend.Services.Data.Interfaces;

/// <summary>
/// Читает методические параметры из parameters.csv.
/// </summary>
public interface IParametersReader
{
    /// <summary>
    /// Читает все параметры, индексируя их по имени.
    /// </summary>
    /// <returns>Словарь параметров по имени.</returns>
    IReadOnlyDictionary<string, MethodologyParameter> ReadParameters();
}
