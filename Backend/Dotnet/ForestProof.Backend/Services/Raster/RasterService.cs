using System.Globalization;
using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Domain.Raster;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Raster.Interfaces;
using Microsoft.Extensions.Options;
using OSGeo.GDAL;

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
