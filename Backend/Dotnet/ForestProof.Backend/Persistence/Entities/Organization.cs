using System.ComponentModel.DataAnnotations;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Организация.
/// </summary>
public sealed class Organization
{
    /// <summary>
    /// Идентификатор.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Название.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Пользователи организации.
    /// </summary>
    public ICollection<User> Users { get; set; } = [];

    /// <summary>
    /// Проекты организации.
    /// </summary>
    public ICollection<Project> Projects { get; set; } = [];
}
