using ForestProof.Backend.Domain.ChangeZones;
using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.ChangeZones.Interfaces;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;

namespace ForestProof.Backend.Services.ChangeZones;

/// <summary>
/// Выделяет связные зоны изменений биомассы по порогу.
/// </summary>
/// <param name="options">Порог чувствительности, минимальная площадь и коэффициент CF.</param>
public sealed class ChangeZoneDetector(IOptions<CalculationOptions> options) : IChangeZoneDetector
{
    private readonly CalculationOptions _options = options.Value;
    private readonly GeometryFactory _geometryFactory = new();

    /// <inheritdoc />
    public IReadOnlyList<ChangeZone> Detect(IReadOnlyList<ChangePixel> pixels, RasterGrid grid)
    {
        var candidates = pixels
            .Where(pixel => Math.Abs(pixel.BiomassChange) > _options.ChangeDetectionThresholdTonnesPerHectare)
            .ToDictionary(pixel => (pixel.Row, pixel.Column));

        var visited = new HashSet<(int Row, int Column)>();
        var zones = new List<ChangeZone>();
        var nextId = 1;

        foreach (var start in candidates.Keys)
        {
            if (visited.Contains(start))
                continue;

            var component = CollectComponent(start, candidates, visited);
            var areaHectares = component.Sum(pixel => pixel.AreaHectares);
            if (areaHectares <= _options.MinChangeZoneAreaHectares)
                continue;

            if (!component.Any(pixel => pixel.HasConfirmation))
                continue;

            zones.Add(new ChangeZone
            {
                Id = nextId++,
                Geometry = BuildGeometry(component, grid),
                AreaHectares = areaHectares,
                ContributionToDeltaCarbon = component.Sum(pixel =>
                    pixel.BiomassChange * _options.CarbonFraction * pixel.AreaHectares),
                PixelCount = component.Count,
                Pixels = component
            });
        }

        return zones;
    }

    private Geometry BuildGeometry(IReadOnlyList<ChangePixel> component, RasterGrid grid)
    {
        var polygons = component
            .Select(pixel => CreatePixelPolygon(grid, pixel.Row, pixel.Column))
            .ToArray();

        return UnaryUnionOp.Union(polygons);
    }

    private Geometry CreatePixelPolygon(RasterGrid grid, int row, int column)
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

    private static List<ChangePixel> CollectComponent(
        (int Row, int Column) start,
        IReadOnlyDictionary<(int Row, int Column), ChangePixel> candidates,
        HashSet<(int Row, int Column)> visited)
    {
        var component = new List<ChangePixel>();
        var queue = new Queue<(int Row, int Column)>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            component.Add(candidates[current]);

            foreach (var neighbor in Neighbors(current))
            {
                if (candidates.ContainsKey(neighbor) && visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        return component;
    }

    private static IEnumerable<(int Row, int Column)> Neighbors((int Row, int Column) cell)
    {
        yield return (cell.Row - 1, cell.Column);
        yield return (cell.Row + 1, cell.Column);
        yield return (cell.Row, cell.Column - 1);
        yield return (cell.Row, cell.Column + 1);
    }
}
