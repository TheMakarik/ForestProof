using System.Globalization;
using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Domain.Raster;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Raster.Interfaces;
using Microsoft.Extensions.Options;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace ForestProof.Backend.Services.Raster;

/// <summary>
/// Читает растры биомассы ESA CCI Biomass через GDAL.
/// </summary>
/// <param name="options">Пути к файлам набора данных.</param>
public sealed class RasterService(IOptions<DataOptions> options) : IRasterService
{
    private const int BiomassBandIndex = 1;
    private const int StandardDeviationBandIndex = 2;
    private const string YearPlaceholder = "{year}";

    private readonly DataOptions _options = options.Value;

    /// <inheritdoc />
    public BiomassWindow ReadBiomass(string aoiId, int year)
    {
        GdalInitializer.EnsureInitialized();

        using var dataset = Gdal.Open(ResolveBiomassPath(aoiId, year), Access.GA_ReadOnly);
        if (dataset.RasterCount < StandardDeviationBandIndex)
            throw new InvalidDataException(
                $"Растр биомассы '{aoiId}/{year}' должен содержать AGB и AGB_SD.");

        ValidateWgs84Crs(dataset, $"{aoiId}/{year}");

        var grid = ReadGrid(dataset);
        var biomass = ReadBand(dataset.GetRasterBand(BiomassBandIndex), grid);
        var standardDeviation = ReadBand(dataset.GetRasterBand(StandardDeviationBandIndex), grid);

        return new BiomassWindow
        {
            Grid = grid,
            Biomass = biomass,
            StandardDeviation = standardDeviation,
            ValidPixelCount = biomass.Count(value => value.HasValue)
        };
    }

    /// <inheritdoc />
    public GfcWindow ReadGfc(string aoiId)
    {
        GdalInitializer.EnsureInitialized();

        using var dataset = Gdal.Open(ResolveGfcPath(aoiId), Access.GA_ReadOnly);
        if (dataset.RasterCount < 2)
            throw new InvalidDataException(
                $"Растр GFC '{aoiId}' должен содержать treecover2000 и lossyear.");

        ValidateWgs84Crs(dataset, aoiId);

        var grid = ReadGrid(dataset);
        var treeCover = ReadBand(dataset.GetRasterBand(1), grid);
        var lossYear = ReadBand(dataset.GetRasterBand(2), grid);

        return new GfcWindow
        {
            Grid = grid,
            TreeCover = treeCover.Select(value => value is { } number ? (int?)number : null).ToArray(),
            LossYear = lossYear.Select(value => value is { } number ? (int?)number : null).ToArray()
        };
    }

    /// <inheritdoc />
    public RasterBandsWindow ReadBands(string aoiId, string relativePath, IReadOnlyList<int> bandIndexes)
    {
        GdalInitializer.EnsureInitialized();

        using var dataset = Gdal.Open(Path.Join(_options.DataRoot, aoiId, relativePath), Access.GA_ReadOnly);
        var grid = ReadGrid(dataset);

        var bands = bandIndexes
            .Select(index => (IReadOnlyList<double?>)ReadBand(dataset.GetRasterBand(index), grid))
            .ToArray();

        return new RasterBandsWindow
        {
            Grid = grid,
            Bands = bands
        };
    }

    /// <inheritdoc />
    public ChangeWindow ReadChange(string aoiId)
    {
        GdalInitializer.EnsureInitialized();

        using var dataset = Gdal.Open(
            Path.Join(_options.DataRoot, aoiId, _options.ChangeRasterFileName),
            Access.GA_ReadOnly);

        if (dataset.RasterCount < StandardDeviationBandIndex)
            throw new InvalidDataException(
                $"Растр изменения '{aoiId}' должен содержать AGB_difference и AGB_difference_SD.");

        var grid = ReadGrid(dataset);
        var difference = ReadBand(dataset.GetRasterBand(1), grid);
        var standardDeviation = ReadBand(dataset.GetRasterBand(2), grid);
        var qualityFlag = dataset.RasterCount >= 3
            ? ReadBand(dataset.GetRasterBand(3), grid)
            : new double?[grid.Width * grid.Height];

        return new ChangeWindow
        {
            Grid = grid,
            AgbDifference = difference,
            StandardDeviation = standardDeviation,
            QualityFlag = qualityFlag.Select(value => value is { } number ? (int?)number : null).ToArray()
        };
    }

    /// <inheritdoc />
    public int? GetModisBurnYear(string aoiId)
    {
        var directory = Path.Join(_options.DataRoot, aoiId, _options.ModisDirectoryName);
        if (!Directory.Exists(directory))
            return null;

        var file = Directory.GetFiles(directory, "*_Burn_Date.tif").FirstOrDefault();
        if (file is null)
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(
            Path.GetFileName(file),
            @"A(\d{4})\d{3}");

        return match.Success && int.TryParse(match.Groups[1].Value, out var year) ? year : null;
    }

    /// <inheritdoc />
    public double? SampleModisBurnDate(string aoiId, double longitude, double latitude)
    {
        var directory = Path.Join(_options.DataRoot, aoiId, _options.ModisDirectoryName);
        if (!Directory.Exists(directory))
            return null;

        var file = Directory.GetFiles(directory, "*_Burn_Date.tif").FirstOrDefault();
        return file is null ? null : SampleRaster(file, 1, longitude, latitude);
    }

    /// <inheritdoc />
    public (int Row, int Column)? TransformToPixel(
        string aoiId,
        string relativePath,
        double longitude,
        double latitude)
    {
        GdalInitializer.EnsureInitialized();

        using var dataset = Gdal.Open(
            Path.Join(_options.DataRoot, aoiId, relativePath),
            Access.GA_ReadOnly);

        return TransformToPixel(dataset, longitude, latitude);
    }

    private double? SampleRaster(string path, int bandIndex, double longitude, double latitude)
    {
        GdalInitializer.EnsureInitialized();

        using var dataset = Gdal.Open(path, Access.GA_ReadOnly);
        if (TransformToPixel(dataset, longitude, latitude) is not { } pixel)
            return null;

        var buffer = new double[1];
        dataset.GetRasterBand(bandIndex).ReadRaster(pixel.Column, pixel.Row, 1, 1, buffer, 1, 1, 0, 0);
        return double.IsFinite(buffer[0]) ? buffer[0] : null;
    }

    private static (int Row, int Column)? TransformToPixel(Dataset dataset, double longitude, double latitude)
    {
        var target = dataset.GetSpatialRef();
        if (target is null)
            return null;

        using var source = new SpatialReference("");
        source.ImportFromEPSG(4326);
        source.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
        target.SetAxisMappingStrategy(AxisMappingStrategy.OAMS_TRADITIONAL_GIS_ORDER);
        using var transformation = new CoordinateTransformation(source, target);

        var point = new[] { longitude, latitude, 0.0 };
        transformation.TransformPoint(point);

        var geoTransform = new double[6];
        dataset.GetGeoTransform(geoTransform);
        var column = (int)Math.Floor((point[0] - geoTransform[0]) / geoTransform[1]);
        var row = (int)Math.Floor((point[1] - geoTransform[3]) / geoTransform[5]);

        if (row < 0 || row >= dataset.RasterYSize || column < 0 || column >= dataset.RasterXSize)
            return null;

        return (row, column);
    }

    private static void ValidateWgs84Crs(Dataset dataset, string source)
    {
        var spatialReference = dataset.GetSpatialRef();
        if (spatialReference is null)
            throw new InvalidDataException($"Растр '{source}' не содержит системы координат.");

        spatialReference.AutoIdentifyEPSG();
        var authorityCode = spatialReference.GetAuthorityCode(null);
        if (authorityCode != "4326")
            throw new InvalidDataException(
                $"Растр '{source}' должен быть в EPSG:4326, получено EPSG:{authorityCode}.");
    }

    private static RasterGrid ReadGrid(Dataset dataset)
    {
        var geoTransform = new double[6];
        dataset.GetGeoTransform(geoTransform);

        return new RasterGrid
        {
            OriginLongitude = geoTransform[0],
            OriginLatitude = geoTransform[3],
            PixelWidthDegrees = geoTransform[1],
            PixelHeightDegrees = Math.Abs(geoTransform[5]),
            Width = dataset.RasterXSize,
            Height = dataset.RasterYSize
        };
    }

    private static double?[] ReadBand(Band band, RasterGrid grid)
    {
        var pixelCount = grid.Width * grid.Height;
        var values = new double[pixelCount];
        band.ReadRaster(0, 0, grid.Width, grid.Height, values, grid.Width, grid.Height, 0, 0);
        band.GetNoDataValue(out double noData, out int hasNoData);

        var result = new double?[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            var value = values[i];
            var isMissing = double.IsNaN(value) ||
                            double.IsInfinity(value) ||
                            (hasNoData != 0 && value == noData);
            result[i] = isMissing ? null : value;
        }

        return result;
    }

    private string ResolveBiomassPath(string aoiId, int year)
    {
        var fileName = _options.BiomassRasterFileNamePattern.Replace(
            YearPlaceholder,
            year.ToString(CultureInfo.InvariantCulture));

        return Path.Join(_options.DataRoot, aoiId, fileName);
    }

    private string ResolveGfcPath(string aoiId) =>
        Path.Join(_options.DataRoot, aoiId, _options.GfcRasterFileName);
}
