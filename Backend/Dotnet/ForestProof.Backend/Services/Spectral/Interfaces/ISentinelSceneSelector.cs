using ForestProof.Backend.Domain.Spectral;

namespace ForestProof.Backend.Services.Spectral.Interfaces;

/// <summary>
/// Выбирает пару сопоставимых летних сцен Sentinel-2 до и после изменения.
/// </summary>
public interface ISentinelSceneSelector
{
    /// <summary>
    /// Подбирает сцены, ближайшие к середине лета начального и конечного годов.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI.</param>
    /// <param name="startYear">Начальный год периода.</param>
    /// <param name="endYear">Конечный год периода.</param>
    /// <returns>Пара сцен или null, если подходящих сцен нет.</returns>
    SentinelScenePair? SelectPair(string aoiId, int startYear, int endYear);
}
