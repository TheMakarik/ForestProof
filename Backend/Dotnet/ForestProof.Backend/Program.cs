using ForestProof.Backend.Endpoints;
using ForestProof.Backend.Extensions;
using ForestProof.Backend.Options;
using ForestProof.Backend.Persistence;
using ForestProof.Backend.Services.Calculation.Interfaces;
using Microsoft.EntityFrameworkCore;
using Scrutor;

var builder = WebApplication.CreateBuilder(args);

builder.AddMethodologyParametersFromCsv();

builder.AddOptions<CalculationOptions>();
builder.AddOptions<DataOptions>();
builder.AddOptions<GeometryOptions>();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("ForestProof")
    ?? throw new InvalidOperationException("Строка подключения 'ForestProof' не задана.");

builder.Services.AddDbContextFactory<ForestProofDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));

builder.Services.AddSingleton<ForestProof.Backend.Persistence.Interfaces.IAnalysisRunRepository,
    ForestProof.Backend.Persistence.AnalysisRunRepository>();

builder.Services.Scan(selector => selector
    .FromAssemblyOf<ICarbonCalculator>()
    .AddClasses(classes => classes.Where(type =>
        type.Namespace is not null &&
        type.Namespace.StartsWith("ForestProof.Backend.Services", StringComparison.Ordinal)))
    .AsImplementedInterfaces()
    .WithSingletonLifetime());

var app = builder.Build();

await MigrateDatabaseAsync(app);

app.MapOpenApi();

app.MapGet("/", () => "Hello World!");
app.MapAnalysisEndpoints();
app.MapStatusEndpoints();
app.MapRegistryEndpoints();

app.Run();

static async Task MigrateDatabaseAsync(WebApplication application)
{
    const int maxAttempts = 5;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await using var scope = application.Services.CreateAsyncScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ForestProofDbContext>>();
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            await dbContext.Database.MigrateAsync();
            return;
        }
        catch (Exception exception)
        {
            application.Logger.LogWarning(
                exception,
                "Не удалось применить миграции базы данных (попытка {Attempt}/{MaxAttempts}); сервис продолжит работу без БД.",
                attempt,
                maxAttempts);

            if (attempt < maxAttempts)
                await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

public partial class Program;