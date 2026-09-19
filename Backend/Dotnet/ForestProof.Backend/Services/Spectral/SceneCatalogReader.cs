using System.Globalization;
using ForestProof.Backend.Services.Data;
using ForestProof.Backend.Services.Spectral.Interfaces;

namespace ForestProof.Backend.Services.Spectral;

/// <summary>
/// Читает каталог сцен Sentinel-2 из scenes.csv.
/// </summary>
public sealed class SceneCatalogReader : ISceneCatalogReader
{
    private const string AoiIdColumn = "aoi_id";
    private const string SceneKeyColumn = "scene_key";
    private const string ItemIdColumn = "item_id";
    private const string AcquiredAtColumn = "datetime_utc";
    private const string CloudPercentColumn = "source_scene_cloud_percent";
    private const string ValidSclFractionColumn = "scl_4_5_6_7_fraction_crop";
    private const string ReflectancePathColumn = "reflectance_path";
    private const string SclPathColumn = "scl_path";

    /// <inheritdoc />
    public IReadOnlyCollection<SceneCatalogEntry> Read(string scenesCsvPath, string aoiId)
    {
        if (string.IsNullOrWhiteSpace(aoiId) || !File.Exists(scenesCsvPath))
            return Array.Empty<SceneCatalogEntry>();

        try
        {
            var document = new CsvDocument(File.ReadAllText(scenesCsvPath));
            var entries = new List<SceneCatalogEntry>();

            foreach (var row in document.Rows)
            {
                if (!string.Equals(document.GetString(row, AoiIdColumn), aoiId, StringComparison.Ordinal))
                    continue;

                if (!TryParseDate(document.GetString(row, AcquiredAtColumn), out var date))
                    continue;

                entries.Add(new SceneCatalogEntry
                {
                    SceneKey = document.GetString(row, SceneKeyColumn),
                    ItemId = document.GetString(row, ItemIdColumn),
                    Date = date,
                    CloudPercent = document.GetDouble(row, CloudPercentColumn),
                    ValidSclFraction = document.GetDouble(row, ValidSclFractionColumn),
                    ReflectancePath = document.GetString(row, ReflectancePathColumn),
                    SclPath = document.GetString(row, SclPathColumn)
                });
            }

            return entries;
        }
        catch (FormatException)
        {
            return Array.Empty<SceneCatalogEntry>();
        }
        catch (IOException)
        {
            return Array.Empty<SceneCatalogEntry>();
        }
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        date = default;
        if (value.Length < 10)
            return false;

        return DateOnly.TryParseExact(
            value[..10],
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }
}
