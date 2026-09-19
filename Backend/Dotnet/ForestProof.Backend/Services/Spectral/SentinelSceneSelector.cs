using ForestProof.Backend.Domain.Spectral;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Spectral.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Spectral;

/// <summary>
/// Выбирает пару сопоставимых летних сцен Sentinel-2 до и после изменения.
/// </summary>
/// <param name="options">Пути к файлам набора данных.</param>
public sealed class SentinelSceneSelector(IOptions<DataOptions> options) : ISentinelSceneSelector
{
    private const string ReflectanceSuffix = "_reflectance.tif";
    private const string SclSuffix = "_SCL.tif";
    private const string ScenesCsvFileName = "scenes.csv";
    private const int SummerMonth = 7;
    private const int SummerDay = 15;
    private const int SummerFirstMonth = 6;
    private const int SummerLastMonth = 8;

    private readonly DataOptions _options = options.Value;
    private readonly ISceneCatalogReader _sceneCatalogReader = new SceneCatalogReader();

    /// <inheritdoc />
    public SentinelScenePair? SelectPair(string aoiId, int startYear, int endYear)
    {
        var directory = Path.Join(_options.DataRoot, aoiId, _options.SentinelDirectoryName);
        if (!Directory.Exists(directory))
            return null;

        var metrics = ReadSceneMetrics(aoiId);

        var scenes = Directory
            .GetFiles(directory, "*" + ReflectanceSuffix)
            .Select(file => Path.GetFileName(file))
            .Select(file => (File: file, Date: ParseDate(file)))
            .Where(scene => scene.Date.HasValue)
            .Select(scene => ToCandidate(scene.File, scene.Date!.Value, metrics))
            .ToArray();

        if (scenes.Length == 0)
            return null;

        var before = SelectScene(scenes, startYear);
        var after = SelectScene(scenes, endYear);
        if (before.File == after.File)
            return null;

        return new SentinelScenePair
        {
            BeforeReflectanceFileName = Path.Join(_options.SentinelDirectoryName, before.File),
            BeforeSclFileName = Path.Join(_options.SentinelDirectoryName, ToSclFileName(before.File)),
            BeforeYear = before.Date.Year,
            AfterReflectanceFileName = Path.Join(_options.SentinelDirectoryName, after.File),
            AfterSclFileName = Path.Join(_options.SentinelDirectoryName, ToSclFileName(after.File)),
            AfterYear = after.Date.Year
        };
    }

    private IReadOnlyDictionary<string, SceneCatalogEntry> ReadSceneMetrics(string aoiId)
    {
        var scenesCsvPath = Path.Join(_options.DataRoot, ScenesCsvFileName);
        return _sceneCatalogReader
            .Read(scenesCsvPath, aoiId)
            .GroupBy(entry => entry.ItemId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
    }

    private static SceneCandidate ToCandidate(
        string file,
        DateOnly date,
        IReadOnlyDictionary<string, SceneCatalogEntry> metrics)
    {
        var itemId = file.EndsWith(ReflectanceSuffix, StringComparison.Ordinal)
            ? file[..^ReflectanceSuffix.Length]
            : file;

        return metrics.TryGetValue(itemId, out var entry)
            ? new SceneCandidate(file, date, entry.ValidSclFraction, entry.CloudPercent)
            : new SceneCandidate(file, date, null, null);
    }

    private static SceneCandidate SelectScene(IReadOnlyList<SceneCandidate> scenes, int year)
    {
        var target = new DateOnly(year, SummerMonth, SummerDay);

        var summerCandidates = scenes
            .Where(scene =>
                scene.Date.Year == year &&
                scene.Date.Month >= SummerFirstMonth &&
                scene.Date.Month <= SummerLastMonth &&
                scene.ValidSclFraction.HasValue)
            .ToArray();

        if (summerCandidates.Length > 0)
        {
            return summerCandidates
                .OrderByDescending(scene => scene.ValidSclFraction!.Value)
                .ThenBy(scene => scene.CloudPercent ?? double.MaxValue)
                .ThenBy(scene => Math.Abs(scene.Date.DayNumber - target.DayNumber))
                .First();
        }

        return scenes
            .OrderBy(scene => Math.Abs(scene.Date.DayNumber - target.DayNumber))
            .First();
    }

    private static DateOnly? ParseDate(string fileName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(fileName, @"_(\d{8})_");
        if (!match.Success)
            return null;

        var value = match.Groups[1].Value;
        var year = int.Parse(value[..4]);
        var month = int.Parse(value.Substring(4, 2));
        var day = int.Parse(value.Substring(6, 2));

        return new DateOnly(year, month, day);
    }

    private static string ToSclFileName(string reflectanceFileName) =>
        reflectanceFileName.Replace(ReflectanceSuffix, SclSuffix);

    private readonly record struct SceneCandidate(
        string File,
        DateOnly Date,
        double? ValidSclFraction,
        double? CloudPercent);
}
