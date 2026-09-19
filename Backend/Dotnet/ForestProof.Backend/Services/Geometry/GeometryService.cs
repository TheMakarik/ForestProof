using DotSpatial.Projections;
using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Geometry.Interfaces;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NtsGeometry = NetTopologySuite.Geometries.Geometry;

namespace ForestProof.Backend.Services.Geometry;

/// <summary>
/// Считает площади пересечения пикселей растровой сетки с полигоном.
/// </summary>
/// <param name="options">Исходная и целевая (равновеликая метрическая) системы координат.</param>
public sealed class GeometryService(IOptions<GeometryOptions> options) : IGeometryService
{
    private const double SquareMetersPerHectare = 10_000;

    private readonly GeometryFactory _geometryFactory = new();
    private readonly ProjectionInfo _sourceProjection = ProjectionInfo.FromEpsgCode(options.Value.SourceEpsgCode);
    private readonly ProjectionInfo _targetProjection = ProjectionInfo.FromEpsgCode(options.Value.TargetEpsgCode);

    /// <inheritdoc />
    public PixelAreaResult CalculatePixelAreas(NtsGeometry polygon, RasterGrid grid)
    {
        if (grid.PixelWidthDegrees <= 0 || grid.PixelHeightDegrees <= 0)
            throw new ArgumentException("Размер пикселя должен быть положительным.", nameof(grid));

        var envelope = polygon.EnvelopeInternal;
        if (envelope.MinX < -180 || envelope.MaxX > 180 || envelope.MinY < -90 || envelope.MaxY > 90)
            throw new ArgumentException("Координаты полигона должны быть в диапазоне WGS 84.", nameof(polygon));

        var warnings = new List<string>();
        var validPolygon = EnsureValid(polygon, warnings);
        WarnIfOutsideGrid(validPolygon, grid, warnings);
        var targetPolygon = Transform(validPolygon);
        var polygonAreaHectares = targetPolygon.Area / SquareMetersPerHectare;

        var range = GetCandidateRange(validPolygon, grid);
        var pixels = new List<PixelIntersection>();
        var totalAreaHectares = 0.0;

        for (var row = range.MinRow; row <= range.MaxRow; row++)
        {
            for (var column = range.MinColumn; column <= range.MaxColumn; column++)
            {
                var pixelPolygon = Transform(CreatePixelPolygon(grid, row, column));
                var intersection = targetPolygon.Intersection(pixelPolygon);
                if (intersection.IsEmpty)
                    continue;

                var areaHectares = intersection.Area / SquareMetersPerHectare;
                if (areaHectares <= 0)
                    continue;

                pixels.Add(new PixelIntersection
                {
                    Row = row,
                    Column = column,
                    AreaHectares = areaHectares
                });
                totalAreaHectares += areaHectares;
            }
        }

        return new PixelAreaResult
        {
            PolygonAreaHectares = polygonAreaHectares,
            TotalAreaHectares = totalAreaHectares,
            Pixels = pixels,
            Warnings = warnings
        };
    }

    private static void WarnIfOutsideGrid(NtsGeometry polygon, RasterGrid grid, ICollection<string> warnings)
    {
        var envelope = polygon.EnvelopeInternal;
        var west = grid.OriginLongitude;
        var east = grid.OriginLongitude + grid.Width * grid.PixelWidthDegrees;
        var north = grid.OriginLatitude;
        var south = grid.OriginLatitude - grid.Height * grid.PixelHeightDegrees;

        if (envelope.MinX < west || envelope.MaxX > east || envelope.MinY < south || envelope.MaxY > north)
            warnings.Add("Контур частично выходит за покрытие растра; учтена только доступная часть.");
    }

    private static NtsGeometry EnsureValid(NtsGeometry polygon, ICollection<string> warnings)
    {
        if (polygon.IsValid)
            return polygon;

        warnings.Add("Геометрия полигона содержала самопересечения и была исправлена.");
        return polygon.Buffer(0);
    }

    private NtsGeometry Transform(NtsGeometry geometry) => geometry switch
    {
        Polygon polygon => TransformPolygon(polygon),
        MultiPolygon multiPolygon => _geometryFactory.CreateMultiPolygon(
            multiPolygon.Geometries.Select(item => TransformPolygon((Polygon)item)).ToArray()),
        _ => throw new NotSupportedException($"Тип геометрии {geometry.GeometryType} не поддерживается.")
    };

    private Polygon TransformPolygon(Polygon polygon)
    {
        var shell = TransformRing(polygon.ExteriorRing);
        var holes = new LinearRing[polygon.NumInteriorRings];

        for (var i = 0; i < holes.Length; i++)
            holes[i] = TransformRing(polygon.GetInteriorRingN(i));

        return _geometryFactory.CreatePolygon(shell, holes);
    }

    private LinearRing TransformRing(LineString ring)
    {
        var coordinates = new Coordinate[ring.NumPoints];
        for (var i = 0; i < ring.NumPoints; i++)
            coordinates[i] = ring.GetCoordinateN(i);

        return _geometryFactory.CreateLinearRing(TransformCoordinates(coordinates));
    }

    private Coordinate[] TransformCoordinates(IReadOnlyList<Coordinate> coordinates)
    {
        var xy = new double[coordinates.Count * 2];
        for (var i = 0; i < coordinates.Count; i++)
        {
            xy[i * 2] = coordinates[i].X;
            xy[i * 2 + 1] = coordinates[i].Y;
        }

        var z = new double[coordinates.Count];
        Reproject.ReprojectPoints(xy, z, _sourceProjection, _targetProjection, 0, coordinates.Count);

        var transformed = new Coordinate[coordinates.Count];
        for (var i = 0; i < coordinates.Count; i++)
            transformed[i] = new Coordinate(xy[i * 2], xy[i * 2 + 1]);

        return transformed;
    }

    private NtsGeometry CreatePixelPolygon(RasterGrid grid, int row, int column)
    {
        var minLongitude = grid.OriginLongitude + column * grid.PixelWidthDegrees;
        var maxLongitude = minLongitude + grid.PixelWidthDegrees;
        var maxLatitude = grid.OriginLatitude - row * grid.PixelHeightDegrees;
        var minLatitude = maxLatitude - grid.PixelHeightDegrees;

        return _geometryFactory.CreatePolygon(
        [
            new Coordinate(minLongitude, minLatitude),
            new Coordinate(maxLongitude, minLatitude),
            new Coordinate(maxLongitude, maxLatitude),
            new Coordinate(minLongitude, maxLatitude),
            new Coordinate(minLongitude, minLatitude)
        ]);
    }

    private static (int MinColumn, int MaxColumn, int MinRow, int MaxRow) GetCandidateRange(
        NtsGeometry polygon,
        RasterGrid grid)
    {
        var envelope = polygon.EnvelopeInternal;
        var minColumn = (int)Math.Floor((envelope.MinX - grid.OriginLongitude) / grid.PixelWidthDegrees);
        var maxColumn = (int)Math.Floor((envelope.MaxX - grid.OriginLongitude) / grid.PixelWidthDegrees);
        var minRow = (int)Math.Floor((grid.OriginLatitude - envelope.MaxY) / grid.PixelHeightDegrees);
        var maxRow = (int)Math.Floor((grid.OriginLatitude - envelope.MinY) / grid.PixelHeightDegrees);

        return (
            Math.Clamp(minColumn, 0, grid.Width - 1),
            Math.Clamp(maxColumn, 0, grid.Width - 1),
            Math.Clamp(minRow, 0, grid.Height - 1),
            Math.Clamp(maxRow, 0, grid.Height - 1));
    }
}
