using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForestProof.Backend.Persistence.Enums;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Отчёт.
/// </summary>
public sealed class ReportEntity
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор запуска.
    /// </summary>
    public Guid RunId { get; set; }

    /// <summary>
    /// Формат отчёта.
    /// </summary>
    public ReportFormat Format { get; set; }

    /// <summary>
    /// Ссылка на отчёт.
    /// </summary>
    [MaxLength(512)]
    public string? Uri { get; set; }

    /// <summary>
    /// Запуск.
    /// </summary>
    [ForeignKey(nameof(RunId))]
    public AnalysisRun? Run { get; set; }
}
