using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ForestProof.Backend.Persistence;

/// <summary>
/// Фабрика контекста для команд миграций EF Core.
/// </summary>
public sealed class ForestProofDbContextFactory : IDesignTimeDbContextFactory<ForestProofDbContext>
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=forestproof;Username=forestproof;Password=forestproof";

    /// <inheritdoc />
    public ForestProofDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FORESTPROOF_DB") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<ForestProofDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new ForestProofDbContext(options);
    }
}
