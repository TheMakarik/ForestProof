using System.Globalization;
using CsvHelper;
using ForestProof.Backend.Options;

namespace ForestProof.Backend.Extensions;

/// <summary>
/// Переопределяет параметры методики значениями из parameters.csv.
/// </summary>
public static class ParametersConfigurationExtensions
{
    private const string DataOptionsSectionName = nameof(DataOptions);
    private const string CalculationOptionsSectionName = nameof(CalculationOptions);
    private const string ParameterColumnName = "parameter";
    private const string ValueColumnName = "value";

    private static readonly IReadOnlyDictionary<string, string> ParameterToConfigurationKey =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CF_AGB"] = $"{CalculationOptionsSectionName}:CarbonFraction",
            ["CO2_per_C"] = $"{CalculationOptionsSectionName}:Co2PerCarbonRatio",
            ["UNC_allowance"] = $"{CalculationOptionsSectionName}:UncertaintyThreshold",
            ["BUF"] = $"{CalculationOptionsSectionName}:ReserveFraction",
            ["LK"] = $"{CalculationOptionsSectionName}:LeakageTonnesCo2",
            ["price_low"] = $"{CalculationOptionsSectionName}:PriceScenariosRubles:0",
            ["price_base"] = $"{CalculationOptionsSectionName}:PriceScenariosRubles:1",
            ["price_high"] = $"{CalculationOptionsSectionName}:PriceScenariosRubles:2",
            ["history_start_year"] = $"{CalculationOptionsSectionName}:BaselineHistoricalYear",
            ["history_end_year"] = $"{CalculationOptionsSectionName}:BaselineReferenceYear"
        };

    /// <summary>
    /// Читает parameters.csv и добавляет найденные значения как override конфигурации.
    /// Метод безопасен: при отсутствии файла или ошибке чтения конфигурация не изменяется.
    /// </summary>
    /// <param name="builder">Построитель приложения.</param>
    /// <returns>Тот же построитель для цепочки вызовов.</returns>
    public static IHostApplicationBuilder AddMethodologyParametersFromCsv(this IHostApplicationBuilder builder)
    {
        var dataSection = builder.Configuration.GetSection(DataOptionsSectionName);
        var dataRoot = dataSection["DataRoot"];
        var methodologyDirectoryName = dataSection["MethodologyDirectoryName"];
        var parametersCsvFileName = dataSection["ParametersCsvFileName"];

        if (string.IsNullOrWhiteSpace(dataRoot) ||
            string.IsNullOrWhiteSpace(methodologyDirectoryName) ||
            string.IsNullOrWhiteSpace(parametersCsvFileName))
        {
            return builder;
        }

        var path = Path.Join(dataRoot, methodologyDirectoryName, parametersCsvFileName);

        try
        {
            if (!File.Exists(path))
                return builder;

            var overrides = BuildConfigurationOverrides(ReadParameterValues(path));
            if (overrides.Count > 0)
                builder.Configuration.AddInMemoryCollection(overrides);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Не удалось применить параметры методики из '{path}': {exception.Message}");
        }

        return builder;
    }

    private static IReadOnlyDictionary<string, string> ReadParameterValues(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!csv.Read() || !csv.ReadHeader())
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var headers = csv.HeaderRecord ?? [];
        var parameterIndex = Array.IndexOf(headers, ParameterColumnName);
        var valueIndex = Array.IndexOf(headers, ValueColumnName);
        if (parameterIndex < 0 || valueIndex < 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        while (csv.Read())
        {
            var parameter = csv.GetField(parameterIndex);
            if (string.IsNullOrWhiteSpace(parameter))
                continue;

            values[parameter] = csv.GetField(valueIndex) ?? string.Empty;
        }

        return values;
    }

    private static Dictionary<string, string?> BuildConfigurationOverrides(
        IReadOnlyDictionary<string, string> values)
    {
        var overrides = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (var (parameter, configurationKey) in ParameterToConfigurationKey)
        {
            if (!values.TryGetValue(parameter, out var rawValue))
                continue;

            var normalized = NormalizeValue(rawValue);
            if (normalized is not null)
                overrides[configurationKey] = normalized;
        }

        return overrides;
    }

    private static string? NormalizeValue(string rawValue)
    {
        var trimmed = rawValue.Trim();
        if (trimmed.Length == 0)
            return null;

        var separator = trimmed.IndexOf('/');
        if (separator > 0)
        {
            var numeratorText = trimmed[..separator];
            var denominatorText = trimmed[(separator + 1)..];

            if (double.TryParse(numeratorText, NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator) &&
                double.TryParse(denominatorText, NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator) &&
                denominator != 0)
            {
                return (numerator / denominator).ToString(CultureInfo.InvariantCulture);
            }
        }

        return trimmed;
    }
}
