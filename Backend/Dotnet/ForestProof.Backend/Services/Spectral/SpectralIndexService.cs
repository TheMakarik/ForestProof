using ForestProof.Backend.Domain.Spectral;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Raster.Interfaces;
using ForestProof.Backend.Services.Spectral.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Spectral;

/// <summary>
/// Считает спектральные индексы Sentinel-2 с учётом SCL-маски.
/// </summary>
/// <param name="rasterService">Чтение каналов растра.</param>
/// <param name="options">Допустимые SCL-классы.</param>
public sealed class SpectralIndexService(
    IRasterService rasterService,
    IOptions<CalculationOptions> options) : ISpectralIndexService
{
    private const double MinimumDenominator = 1e-12;
    private const double RangeTolerance = 1e-9;

    private readonly CalculationOptions _options = options.Value;

    /// <inheritdoc />
    public SpectralIndexWindow Calculate(string aoiId, string reflectanceFileName, string sclFileName) =>
        Calculate(aoiId, reflectanceFileName, sclFileName, _options.AllowedSclClasses);

    /// <inheritdoc />
    public SpectralIndexWindow Calculate(
        string aoiId,
        string reflectanceFileName,
        string sclFileName,
        IReadOnlyCollection<int> allowedSclClasses)
    {
        var reflectance = rasterService.ReadBands(aoiId, reflectanceFileName, [1, 2, 3, 4, 5, 6]);
        var scl = rasterService.ReadBands(aoiId, sclFileName, [1]);

        if (reflectance.Grid.Width != scl.Grid.Width || reflectance.Grid.Height != scl.Grid.Height)
            throw new InvalidDataException("Отражение и SCL должны быть на одной сетке.");

        var pixelCount = reflectance.Grid.Width * reflectance.Grid.Height;
        var ndvi = new double?[pixelCount];
        var ndwi = new double?[pixelCount];
        var nbr = new double?[pixelCount];
        var validPixelCount = 0;

        for (var i = 0; i < pixelCount; i++)
        {
            if (scl.Bands[0][i] is not { } sclClass || !allowedSclClasses.Contains((int)sclClass))
                continue;

            ndvi[i] = NormalizedDifference(reflectance.Bands[3][i], reflectance.Bands[2][i]);
            ndwi[i] = NormalizedDifference(reflectance.Bands[1][i], reflectance.Bands[3][i]);
            nbr[i] = NormalizedDifference(reflectance.Bands[3][i], reflectance.Bands[5][i]);
            validPixelCount++;
        }

        return new SpectralIndexWindow
        {
            Grid = reflectance.Grid,
            Ndvi = ndvi,
            Ndwi = ndwi,
            Nbr = nbr,
            ValidPixelCount = validPixelCount
        };
    }

    /// <inheritdoc />
    public IReadOnlyList<double?> CalculateDnbr(SpectralIndexWindow before, SpectralIndexWindow after)
    {
        var pixelCount = Math.Min(before.Nbr.Count, after.Nbr.Count);
        var result = new double?[pixelCount];

        for (var i = 0; i < pixelCount; i++)
        {
            if (before.Nbr[i] is { } beforeNbr && after.Nbr[i] is { } afterNbr)
                result[i] = beforeNbr - afterNbr;
        }

        return result;
    }

    private static double? NormalizedDifference(double? first, double? second)
    {
        if (first is not { } a || second is not { } b)
            return null;

        var denominator = a + b;
        if (Math.Abs(denominator) < MinimumDenominator)
            return null;

        var value = (a - b) / denominator;
        return Math.Abs(value) > 1 + RangeTolerance ? null : value;
    }
}
