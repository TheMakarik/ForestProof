using ForestProof.Backend.Domain.Analysis;

namespace ForestProof.Backend.Services.Analysis.Interfaces;

/// <summary>
/// Кэш результатов расчёта по ключу повторяемости.
/// </summary>
public interface IAnalysisResultCache
{
    /// <summary>
    /// Пытается получить ранее посчитанный результат.
    /// </summary>
    /// <param name="inputHash">Хэш входных параметров.</param>
    /// <param name="methodVersion">Версия методики.</param>
    /// <param name="summary">Найденный результат.</param>
    /// <returns>true, если результат найден.</returns>
    bool TryGet(string inputHash, string methodVersion, out AnalysisSummary summary);

    /// <summary>
    /// Сохраняет результат в кэш.
    /// </summary>
    /// <param name="summary">Результат расчёта.</param>
    void Store(AnalysisSummary summary);
}
