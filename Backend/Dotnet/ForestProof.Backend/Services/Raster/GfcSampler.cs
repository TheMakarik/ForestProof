using ForestProof.Backend.Domain.Geometry;
using ForestProof.Backend.Domain.Raster;

namespace ForestProof.Backend.Services.Raster;

/// <summary>
/// Сэмплирует растровые слои по координатам пикселя другой сетки в той же CRS.
/// </summary>
public static class GfcSampler
{
    private const int GfcYearOffset = 2000;

    /// <summary>
    /// Проверяет, есть ли в GFC потеря покрова под указанным пикселем сетки AGB.
    /// </summary>
    /// <param name="agbGrid">Сетка, в которой задан пиксель.</param>
    /// <param name="row">Строка пикселя.</param>
    /// <param name="column">Столбец пикселя.</param>
    /// <param name="gfc">Растр Hansen GFC в той же CRS.</param>
    /// <returns>true, если под пикселем зафиксирована потеря покрова.</returns>
    public static bool HasLoss(RasterGrid agbGrid, int row, int column, GfcWindow gfc) =>
        GetLossYear(agbGrid, row, column, gfc) is not null;

    /// <summary>
    /// Возвращает год потери покрова GFC под пикселем сетки AGB.
    /// </summary>
    /// <param name="agbGrid">Сетка, в которой задан пиксель.</param>
    /// <param name="row">Строка пикселя.</param>
    /// <param name="column">Столбец пикселя.</param>
    /// <param name="gfc">Растр Hansen GFC в той же CRS.</param>
    /// <returns>Год потери; null — потери нет или пиксель вне растра.</returns>
    public static int? GetLossYear(RasterGrid agbGrid, int row, int column, GfcWindow gfc)
    {
        var longitude = agbGrid.OriginLongitude + (column + 0.5) * agbGrid.PixelWidthDegrees;
        var latitude = agbGrid.OriginLatitude - (row + 0.5) * agbGrid.PixelHeightDegrees;

        var gfcColumn = (int)Math.Floor((longitude - gfc.Grid.OriginLongitude) / gfc.Grid.PixelWidthDegrees);
        var gfcRow = (int)Math.Floor((gfc.Grid.OriginLatitude - latitude) / gfc.Grid.PixelHeightDegrees);

        if (gfcRow < 0 || gfcRow >= gfc.Grid.Height || gfcColumn < 0 || gfcColumn >= gfc.Grid.Width)
            return null;

        var lossYear = gfc.LossYear[gfcRow * gfc.Grid.Width + gfcColumn];
        return lossYear is > 0 ? lossYear + GfcYearOffset : null;
    }
}
