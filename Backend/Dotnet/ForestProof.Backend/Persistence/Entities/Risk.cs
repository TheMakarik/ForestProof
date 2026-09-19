using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Риск запуска.
/// </summary>
public sealed class Risk
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
    /// Тип риска.
    /// </summary>
    [MaxLength(128)]
    public string? RiskType { get; set; }

    /// <summary>
    /// Сообщение.
    /// </summary>
    [MaxLength(1024)]
    public string? Message { get; set; }

    /// <summary>
    /// Запуск.
    /// </summary>
    [ForeignKey(nameof(RunId))]
    public AnalysisRun? Run { get; set; }
}
