using ForestProof.Backend.Domain.Baseline;

namespace ForestProof.Backend.Services.Data.Interfaces;

/// <summary>
/// Читает методическую базовую линию из baseline.csv.
/// </summary>
public interface IBaselineReader
{
    /// <summary>
    /// Читает все строки базовой линии.
    /// </summary>
    /// <returns>Список строк базовой линии.</returns>
    IReadOnlyCollection<BaselineRecord> ReadBaselines();

    /// <summary>
    /// Читает базовую линию по идентификатору AOI.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <returns>Первая строка базовой линии для указанного AOI.</returns>
    /// <exception cref="KeyNotFoundException">Если базовая линия не найдена.</exception>
    BaselineRecord ReadBaseline(string aoiId);
}
