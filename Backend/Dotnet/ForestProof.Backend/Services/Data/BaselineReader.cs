using ForestProof.Backend.Domain.Baseline;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Data.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Data;

/// <summary>
/// Читает методическую базовую линию из baseline.csv.
/// </summary>
/// <param name="options">Пути к файлам набора данных.</param>
public sealed class BaselineReader(IOptions<DataOptions> options) : IBaselineReader
{
    private readonly DataOptions _options = options.Value;

    /// <inheritdoc />
    public IReadOnlyCollection<BaselineRecord> ReadBaselines()
    {
        var content = File.ReadAllText(ResolveBaselinePath());
        var document = new CsvDocument(content);

        return document.Rows.Select(row => Map(row, document)).ToArray();
    }

    /// <inheritdoc />
    public BaselineRecord ReadBaseline(string aoiId) =>
        ReadBaselines().FirstOrDefault(record => record.AoiId == aoiId)
        ?? throw new KeyNotFoundException($"Базовая линия для AOI '{aoiId}' не найдена.");

    private static BaselineRecord Map(IReadOnlyList<string> row, CsvDocument document) => new()
    {
        BaselineId = document.GetString(row, "baseline_id"),
        AoiId = document.GetString(row, "aoi_id"),
        YearStart = document.GetInt(row, "year_start"),
        YearEnd = document.GetInt(row, "year_end"),
        Pool = document.GetString(row, "pool"),
        ReferenceMean2015 = document.GetDouble(row, "reference_mean_2015_tc_ha"),
        ReferenceMean2019 = document.GetDouble(row, "reference_mean_2019_tc_ha"),
        HistoricalRate = document.GetDouble(row, "historical_rate_tc_ha_yr")
    };

    private string ResolveBaselinePath() =>
        Path.Join(_options.DataRoot, _options.MethodologyDirectoryName, _options.BaselineCsvFileName);
}
