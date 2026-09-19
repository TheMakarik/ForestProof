using ForestProof.Backend.Domain.Methodology;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Data.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Data;

/// <summary>
/// Читает методические параметры из parameters.csv.
/// </summary>
/// <param name="options">Пути к файлам набора данных.</param>
public sealed class ParametersReader(IOptions<DataOptions> options) : IParametersReader
{
    private readonly DataOptions _options = options.Value;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, MethodologyParameter> ReadParameters()
    {
        var content = File.ReadAllText(ResolveParametersPath());
        var document = new CsvDocument(content);

        return document.Rows
            .Select(row => Map(row, document))
            .ToDictionary(parameter => parameter.Name, StringComparer.Ordinal);
    }

    private static MethodologyParameter Map(IReadOnlyList<string> row, CsvDocument document) => new()
    {
        Name = document.GetString(row, "parameter"),
        Value = document.GetString(row, "value"),
        Unit = document.GetString(row, "unit"),
        Kind = document.GetString(row, "kind"),
        SourceId = document.GetString(row, "source_id"),
        Locator = document.GetString(row, "locator"),
        Applicability = document.GetString(row, "applicability")
    };

    private string ResolveParametersPath() =>
        Path.Join(_options.DataRoot, _options.MethodologyDirectoryName, _options.ParametersCsvFileName);
}
