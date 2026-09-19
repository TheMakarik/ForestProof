using System.Linq;
using ForestProof.Backend.Services.Provenance;

namespace ForestProof.Testing.Provenance;

public sealed class SourceCatalogServiceTests
{
    private const string AoiId = "RU_VOLOGDA_02";
    private const string RelativePath = "RU_VOLOGDA_02/CCI_Biomass_2019.tif";

    private static SourceCatalogService CreateService() =>
        new(TestDataOptions.Create(TestDataset.Locate()));

    [Fact]
    public void ReadCatalog_ReturnsEntriesForAoi()
    {
        var service = CreateService();

        var assets = service.ReadCatalog(AoiId);

        assets.Should().NotBeEmpty();
        assets.Should().Contain(asset => asset.RelativePath == RelativePath);
    }

    [Fact]
    public void ComputeSha256_MatchesCatalogValue()
    {
        var service = CreateService();
        var asset = service.ReadCatalog(AoiId).Single(entry => entry.RelativePath == RelativePath);

        var actual = service.ComputeSha256(AoiId, asset.RelativePath);

        actual.Should().Be(asset.Sha256);
    }

    [Fact]
    public void Verify_ReturnsTrueForMatchingHashAndFalseOtherwise()
    {
        var service = CreateService();
        var asset = service.ReadCatalog(AoiId).Single(entry => entry.RelativePath == RelativePath);

        service.Verify(AoiId, asset.RelativePath, asset.Sha256).Should().BeTrue();
        service.Verify(AoiId, asset.RelativePath, new string('0', 64)).Should().BeFalse();
    }
}
