using System.Globalization;
using CsvHelper;

namespace ForestProof.Backend.Services.Data;

internal sealed class CsvDocument
{
    private readonly Dictionary<string, int> _columnIndexes = new(StringComparer.Ordinal);

    public CsvDocument(string content)
    {
        using var reader = new StringReader(content);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        if (!csv.Read() || !csv.ReadHeader())
            throw new FormatException("CSV не содержит заголовков.");

        var headers = csv.HeaderRecord ?? throw new FormatException("CSV не содержит заголовков.");
        for (var i = 0; i < headers.Length; i++)
            _columnIndexes[headers[i]] = i;

        var rows = new List<IReadOnlyList<string>>();
        while (csv.Read())
        {
            var row = new string[headers.Length];
            for (var i = 0; i < headers.Length; i++)
                row[i] = csv.GetField(i) ?? string.Empty;

            rows.Add(row);
        }

        Rows = rows;
    }

    public IReadOnlyList<IReadOnlyList<string>> Rows { get; }

    public string GetString(IReadOnlyList<string> row, string column) => GetValue(row, column);

    public double GetDouble(IReadOnlyList<string> row, string column)
    {
        var value = double.Parse(GetValue(row, column), CultureInfo.InvariantCulture);
        if (!double.IsFinite(value))
            throw new FormatException($"Значение колонки '{column}' должно быть конечным числом.");

        return value;
    }

    public int GetInt(IReadOnlyList<string> row, string column) =>
        int.Parse(GetValue(row, column), CultureInfo.InvariantCulture);

    private string GetValue(IReadOnlyList<string> row, string column)
    {
        if (!_columnIndexes.TryGetValue(column, out var index))
            throw new FormatException($"В CSV отсутствует колонка '{column}'.");

        return index < row.Count ? row[index] : string.Empty;
    }
}
