using ForestProof.Backend.Endpoints;
using ForestProof.Backend.Extensions;
using ForestProof.Backend.Options;
using ForestProof.Backend.Persistence;
using ForestProof.Backend.Services.Calculation.Interfaces;
using Microsoft.EntityFrameworkCore;
using Scrutor;

var builder = WebApplication.CreateBuilder(args);

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

app.MapOpenApi();

app.MapGet("/", () => "Hello World!");
app.MapAnalysisEndpoints();

app.Run();

public partial class Program;