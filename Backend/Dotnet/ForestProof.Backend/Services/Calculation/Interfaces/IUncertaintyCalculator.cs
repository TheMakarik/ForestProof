using ForestProof.Backend.Domain.Pixels;
using ForestProof.Backend.Domain.Uncertainty;

namespace ForestProof.Backend.Services.Calculation.Interfaces;

/// <summary>
/// Считает сценарный диапазон L–U и ширину H по стандартному отклонению биомассы.
/// </summary>
public interface IUncertaintyCalculator
{
    /// <summary>
    /// Рассчитывает диапазон Eproj по границам биомассы на обе даты.
    /// </summary>
    /// <param name="startSamples">Пиксели начальной даты с биомассой, SD и площадью.</param>
    /// <param name="endSamples">Пиксели конечной даты с биомассой, SD и площадью.</param>
    /// <param name="projectEmission">Центральный Eproj, т CO₂-экв.</param>
    /// <param name="sensitivityCoefficient">Коэффициент чувствительности k.</param>
    /// <returns>Нижнюю L, верхнюю U границы, ширину H и использованный k.</returns>
    UncertaintyRange Calculate(
        IReadOnlyCollection<PixelSample> startSamples,
        IReadOnlyCollection<PixelSample> endSamples,
        double projectEmission,
        double sensitivityCoefficient);
}
