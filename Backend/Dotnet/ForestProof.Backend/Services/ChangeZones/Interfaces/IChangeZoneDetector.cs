using ForestProof.Backend.Domain.ChangeZones;

namespace ForestProof.Backend.Services.ChangeZones.Interfaces;

/// <summary>
/// Выделяет связные зоны изменений биомассы по порогу.
/// </summary>
public interface IChangeZoneDetector
{
    /// <summary>
    /// Строит связные компоненты пикселей с изменением выше порога и фильтрует их по площади.
    /// </summary>
    /// <param name="pixels">Пиксели с изменением биомассы.</param>
    /// <returns>Зоны изменений с площадью и вкладом в ΔC.</returns>
    IReadOnlyList<ChangeZone> Detect(IReadOnlyList<ChangePixel> pixels);
}
