using System.ComponentModel.DataAnnotations;

namespace ForestProof.Backend.Options;

/// <summary>
/// Параметры геометрических расчётов и репроекции.
/// </summary>
public sealed class GeometryOptions
{
    /// <summary>
    /// EPSG-код исходной системы координат входной геометрии (по условиям кейса — WGS 84).
    /// </summary>
    [Range(1024, 99999)]
    public required int SourceEpsgCode { get; init; }

    /// <summary>
    /// EPSG-код целевой равновеликой метрической системы координат для расчёта площадей.
    /// </summary>
    [Range(1024, 99999)]
    public required int TargetEpsgCode { get; init; }
}
