using System.Security.Cryptography;
using ForestProof.Backend.Domain.Provenance;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Data;
using ForestProof.Backend.Services.Provenance.Interfaces;
using Microsoft.Extensions.Options;

namespace ForestProof.Backend.Services.Provenance;

/// <summary>
/// Читает каталог исходных файлов и проверяет их контрольные суммы SHA-256.
/// </summary>
/// <param name="options">Пути к файлам набора данных.</param>
public sealed class SourceCatalogService(IOptions<DataOptions> options) : ISourceCatalogService
{
    private const string CatalogFileName = "file_catalog.csv";

    private readonly DataOptions _options = options.Value;

    /// <inheritdoc />
    public IReadOnlyCollection<SourceAsset> ReadCatalog(string aoiId)
    {
        var content = File.ReadAllText(Path.Join(_options.DataRoot, CatalogFileName));
        var document = new CsvDocument(content);

        return document.Rows
            .Where(row => document.GetString(row, "aoi_id") == aoiId)
            .Select(row => new SourceAsset
            {
                AoiId = document.GetString(row, "aoi_id"),
                RelativePath = document.GetString(row, "relative_path"),
                Sha256 = document.GetString(row, "sha256"),
                SizeBytes = long.Parse(document.GetString(row, "size_bytes"))
            })
            .ToArray();
    }

    /// <inheritdoc />
    public string ComputeSha256(string aoiId, string relativePath)
    {
        var path = Path.Join(_options.DataRoot, relativePath);
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <inheritdoc />
    public bool Verify(string aoiId, string relativePath, string expectedSha256)
    {
        var actual = ComputeSha256(aoiId, relativePath);

        return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }
}
