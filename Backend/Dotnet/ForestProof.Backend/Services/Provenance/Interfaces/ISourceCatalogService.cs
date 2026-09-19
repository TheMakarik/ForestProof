using ForestProof.Backend.Domain.Provenance;

namespace ForestProof.Backend.Services.Provenance.Interfaces;

/// <summary>
/// Предоставляет доступ к каталогу исходных файлов и проверяет их контрольные суммы.
/// </summary>
public interface ISourceCatalogService
{
    /// <summary>
    /// Читает записи каталога для указанной территории.
    /// </summary>
    /// <param name="aoiId">Идентификатор территории.</param>
    /// <returns>Коллекция исходных файлов территории.</returns>
    IReadOnlyCollection<SourceAsset> ReadCatalog(string aoiId);

    /// <summary>
    /// Вычисляет контрольную сумму SHA-256 файла территории.
    /// </summary>
    /// <param name="aoiId">Идентификатор территории.</param>
    /// <param name="relativePath">Путь к файлу относительно корня набора данных.</param>
    /// <returns>Контрольная сумма SHA-256 в нижнем регистре.</returns>
    string ComputeSha256(string aoiId, string relativePath);

    /// <summary>
    /// Проверяет, что контрольная сумма файла совпадает с ожидаемым значением.
    /// </summary>
    /// <param name="aoiId">Идентификатор территории.</param>
    /// <param name="relativePath">Путь к файлу относительно корня набора данных.</param>
    /// <param name="expectedSha256">Ожидаемая контрольная сумма SHA-256.</param>
    /// <returns><c>true</c>, если суммы совпадают; иначе <c>false</c>.</returns>
    bool Verify(string aoiId, string relativePath, string expectedSha256);
}
