using ForestProof.Backend.Domain.Analysis;

namespace ForestProof.Backend.Services.Analysis;

/// <summary>
/// Вычисляет хэш входных параметров расчёта для проверки повторяемости.
/// </summary>
public static class AnalysisInputHash
{
    /// <summary>
    /// Считает SHA-256 от канонического представления запроса и коэффициента чувствительности.
    /// </summary>
    /// <param name="request">Запрос расчёта.</param>
    /// <param name="sensitivityCoefficient">Коэффициент чувствительности k.</param>
    /// <returns>Хэш в нижнем регистре.</returns>
    public static string Compute(AnalysisRequest request, double sensitivityCoefficient)
    {
        var canonical = string.Join(
            '|',
            request.AoiId ?? string.Empty,
            request.PolygonGeoJson ?? string.Empty,
            request.StartYear,
            request.EndYear,
            sensitivityCoefficient,
            request.UseExtendedSclClasses ? "true" : "false");

        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(canonical));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
