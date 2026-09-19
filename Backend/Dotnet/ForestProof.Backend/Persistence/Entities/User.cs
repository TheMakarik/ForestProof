using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ForestProof.Backend.Persistence.Enums;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Пользователь.
/// </summary>
public sealed class User
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
    /// Электронная почта.
    /// </summary>
    [Required]
    [MaxLength(320)]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Роль пользователя.
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Организация.
    /// </summary>
    [ForeignKey(nameof(OrganizationId))]
    public Organization? Organization { get; set; }

    /// <summary>
    /// Запущенные пользователем расчёты.
    /// </summary>
    public ICollection<AnalysisRun> AnalysisRuns { get; set; } = [];
}
