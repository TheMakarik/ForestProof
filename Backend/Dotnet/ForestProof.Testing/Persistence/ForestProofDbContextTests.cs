using ForestProof.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ForestProof.Testing.Persistence;

public sealed class ForestProofDbContextTests
{
    [Fact]
    public void Model_WhenBuilt_ContainsExpectedEntitiesAndPostgis()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ForestProofDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=forestproof;Username=forestproof;Password=forestproof",
                npgsql => npgsql.UseNetTopologySuite())
            .Options;

        using var context = new ForestProofDbContext(options);

        // Act
        var entityNames = context.Model.GetEntityTypes().Select(type => type.ClrType.Name).ToArray();

        // Assert
        entityNames.Should().Contain("AnalysisRun");
        entityNames.Should().Contain("CarbonMetrics");
        entityNames.Should().Contain("ChangeZoneEntity");
        entityNames.Should().Contain("ZoneEvidence");
        entityNames.Should().Contain("Area");
        entityNames.Should().Contain("Baseline");
        entityNames.Should().Contain("ReportEntity");
    }
}
