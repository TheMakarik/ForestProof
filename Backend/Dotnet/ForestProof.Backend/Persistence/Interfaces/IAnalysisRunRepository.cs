using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Persistence.Entities;

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

    /// <summary>
    /// Возвращает сохранённый запуск анализа со связанными результатами.
    /// </summary>
    /// <param name="runId">Идентификатор запуска.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Запуск анализа или <see langword="null"/>, если он не найден.</returns>
    Task<AnalysisRun?> GetAsync(Guid runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавляет сформированный отчёт к сохранённому запуску анализа.
    /// </summary>
    /// <param name="runId">Идентификатор запуска.</param>
    /// <param name="format">Формат отчёта: "pdf", "html" или "json".</param>
    /// <param name="uri">Ссылка на отчёт; может отсутствовать.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task SaveReportAsync(Guid runId, string format, string? uri, CancellationToken cancellationToken = default);
}
