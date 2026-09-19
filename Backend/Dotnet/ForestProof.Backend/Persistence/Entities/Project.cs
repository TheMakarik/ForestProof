using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Проект.
/// </summary>
public sealed class Project
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор организации.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Идентификатор AOI.
    /// </summary>
    [MaxLength(64)]
    public string? AoiId { get; set; }

    /// <summary>
    /// Название.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Заявленный результат.
    /// </summary>
    [MaxLength(512)]
    public string? ClaimedResult { get; set; }

    /// <summary>
    /// Организация.
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    public Organization? Organization { get; set; }

    /// <summary>
    /// Территория.
    /// </summary>
    [ForeignKey(nameof(AoiId))]
    public Area? Area { get; set; }

    /// <summary>
    /// Запуски анализа.
    /// </summary>
    public ICollection<AnalysisRun> AnalysisRuns { get; set; } = [];
}
