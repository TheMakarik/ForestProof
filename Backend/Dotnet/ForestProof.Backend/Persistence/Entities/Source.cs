using System.ComponentModel.DataAnnotations;

namespace ForestProof.Backend.Persistence.Entities;

/// <summary>
/// Источник данных.
/// </summary>
public sealed class Source
{
    /// <summary>
    /// Идентификатор источника.
    /// </summary>
    [Key]
    [MaxLength(64)]
    public string SourceId { get; set; } = null!;

    /// <summary>
    /// Название продукта.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string Product { get; set; } = null!;

    /// <summary>
    /// Версия продукта.
    /// </summary>
    [MaxLength(128)]
    public string? Version { get; set; }

    /// <summary>
    /// Ссылка на лицензию.
    /// </summary>
    [MaxLength(512)]
    public string? LicenseUrl { get; set; }

    /// <summary>
    /// Параметры источника.
    /// </summary>
    public ICollection<Parameter> Parameters { get; set; } = [];

    /// <summary>
    /// Файловые активы источника.
    /// </summary>
    public ICollection<SourceAsset> SourceAssets { get; set; } = [];
}
