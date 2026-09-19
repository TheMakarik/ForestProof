using ForestProof.Backend.Domain.Analysis;

namespace ForestProof.Backend.Services.Analysis.Interfaces;

/// <summary>
/// Выполняет полный расчёт по территории и периоду.
/// </summary>
public interface IAnalysisPipeline
{
    /// <summary>
    /// Считает запас, изменение, неопределённость, baseline и потенциальные единицы.
    /// </summary>
    /// <param name="request">Территория и период расчёта.</param>
    /// <returns>Итог расчёта.</returns>
    AnalysisSummary Run(AnalysisRequest request);
}
