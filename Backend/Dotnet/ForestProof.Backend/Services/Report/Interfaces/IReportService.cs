using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Domain.Report;

namespace ForestProof.Backend.Services.Report.Interfaces;

/// <summary>
/// Формирует отчёт по итогам расчёта.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Формирует отчёт в форматах HTML, JSON, PDF и манифест.
    /// </summary>
    /// <param name="summary">Сводка расчёта по территории и периоду.</param>
    /// <returns>Сформированный отчёт.</returns>
    ReportResult Generate(AnalysisSummary summary);
}
