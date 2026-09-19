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
    private const int SummerMonth = 7;
    private const int SummerDay = 15;

    private readonly DataOptions _options = options.Value;

    /// <inheritdoc />
    public SentinelScenePair? SelectPair(string aoiId, int startYear, int endYear)
    {
        var directory = Path.Join(_options.DataRoot, aoiId, _options.SentinelDirectoryName);
        if (!Directory.Exists(directory))
            return null;

        var scenes = Directory
            .GetFiles(directory, "*" + ReflectanceSuffix)
            .Select(file => Path.GetFileName(file))
            .Select(file => (File: file, Date: ParseDate(file)))
            .Where(scene => scene.Date.HasValue)
            .Select(scene => (scene.File, Date: scene.Date!.Value))
            .ToArray();

        if (scenes.Length == 0)
            return null;

        var targetStart = new DateOnly(startYear, SummerMonth, SummerDay);
        var targetEnd = new DateOnly(endYear, SummerMonth, SummerDay);

        var before = scenes.OrderBy(scene => Math.Abs(scene.Date.DayNumber - targetStart.DayNumber)).First();
        var after = scenes.OrderBy(scene => Math.Abs(scene.Date.DayNumber - targetEnd.DayNumber)).First();
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
}
