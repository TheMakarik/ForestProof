using ForestProof.Backend.Domain.Analysis;

namespace ForestProof.Backend.Persistence.Interfaces;

/// <summary>
/// Сохраняет результаты расчёта в базу данных.
/// </summary>
public interface IAnalysisRunRepository
{
    /// <summary>
    /// Сохраняет запуск анализа и связанные результаты.
    /// </summary>
    /// <param name="summary">Итог расчёта.</param>
    /// <param name="request">Запрос расчёта.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Идентификатор сохранённого запуска.</returns>
    Task<Guid> SaveAsync(AnalysisSummary summary, AnalysisRequest request, CancellationToken cancellationToken = default);
}
