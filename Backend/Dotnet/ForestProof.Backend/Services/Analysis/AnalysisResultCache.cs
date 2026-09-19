using System.Collections.Concurrent;
using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Services.Analysis.Interfaces;

namespace ForestProof.Backend.Services.Analysis;

/// <summary>
/// Потокобезопасный кэш результатов расчёта по ключу input_hash + method_version.
/// </summary>
public sealed class AnalysisResultCache : IAnalysisResultCache
{
    private readonly ConcurrentDictionary<string, AnalysisSummary> _cache = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public bool TryGet(string inputHash, string methodVersion, out AnalysisSummary summary)
    {
        if (_cache.TryGetValue(BuildKey(inputHash, methodVersion), out var cached))
        {
            summary = cached;
            return true;
        }

        summary = null!;
        return false;
    }

    /// <inheritdoc />
    public void Store(AnalysisSummary summary) =>
        _cache[BuildKey(summary.InputHash, summary.MethodVersion)] = summary;

    private static string BuildKey(string inputHash, string methodVersion) =>
        $"{inputHash}|{methodVersion}";
}
