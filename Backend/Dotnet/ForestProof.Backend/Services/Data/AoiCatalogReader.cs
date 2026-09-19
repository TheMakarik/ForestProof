using ForestProof.Backend.Domain.Aoi;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Data.Interfaces;
using Microsoft.Extensions.Options;
using NetTopologySuite.Features;
using NetTopologySuite.IO;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace ForestProof.Backend.Services.Data;

/// <summary>
/// Читает каталог территорий и их геометрию из areas.csv и areas.geojson.
/// </summary>
/// <param name="options">Пути к файлам набора данных.</param>
public sealed class AoiCatalogReader(IOptions<DataOptions> options) : IAoiCatalogReader
{
    private readonly DataOptions _options = options.Value;

    /// <inheritdoc />
    public IReadOnlyCollection<AreaOfInterest> ReadAreas()
    {
        var content = File.ReadAllText(ResolveAreasCsvPath());
        var document = new CsvDocument(content);

        return document.Rows
            .Select(row => new AreaOfInterest
            {
                Id = document.GetString(row, "aoi_id"),
                Name = document.GetString(row, "name"),
                Region = document.GetString(row, "region"),
                AnalysisStartYear = document.GetInt(row, "analysis_start_year"),
                AnalysisEndYear = document.GetInt(row, "analysis_end_year"),
                AreaHectares = document.GetDouble(row, "area_ha"),
                SelectionRole = document.GetString(row, "selection_role"),
                ProjectStatus = document.GetString(row, "project_status"),
                BoundingBox = new AoiBoundingBox
                {
                    West = document.GetDouble(row, "bbox_west"),
                    South = document.GetDouble(row, "bbox_south"),
                    East = document.GetDouble(row, "bbox_east"),
                    North = document.GetDouble(row, "bbox_north")
                },
                BaselineId = document.GetString(row, "baseline_id")
            })
            .ToArray();
    }

    /// <inheritdoc />
    public NtsGeometry ReadGeometry(string aoiId)
    {
        var content = File.ReadAllText(ResolveAreasGeoJsonPath());
        var reader = new GeoJsonReader();
        var collection = reader.Read<FeatureCollection>(content);

        foreach (var feature in collection)
        {
            if (feature.Geometry is null || !feature.Attributes.Exists("aoi_id"))
                continue;

            if ((string)feature.Attributes["aoi_id"] == aoiId)
                return feature.Geometry;
        }

        throw new KeyNotFoundException($"AOI '{aoiId}' не найден в {_options.AreasGeoJsonFileName}.");
    }

    private string ResolveAreasCsvPath() => Path.Join(_options.DataRoot, _options.AreasCsvFileName);

    private string ResolveAreasGeoJsonPath() => Path.Join(_options.DataRoot, _options.AreasGeoJsonFileName);
}
