using ForestProof.Backend.Domain.Raster;

namespace ForestProof.Backend.Services.Raster.Interfaces;

/// <summary>
/// Читает растры биомассы ESA CCI Biomass.
/// </summary>
public interface IRasterService
{
    /// <summary>
    /// Читает AGB и AGB_SD за указанный год.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI (папка набора данных).</param>
    /// <param name="year">Год растра.</param>
    /// <returns>Сетку, значения AGB и стандартного отклонения, число валидных пикселей.</returns>
    BiomassWindow ReadBiomass(string aoiId, int year);

    /// <summary>
    /// Читает растровые слои Hansen GFC: древесный покров и год потери.
    /// </summary>
    /// <param name="aoiId">Идентификатор AOI (папка набора данных).</param>
    /// <returns>Сетку, древесный покров и год потери по пикселям.</returns>
    GfcWindow ReadGfc(string aoiId);
}
