using ForestProof.Backend.Extensions;
using ForestProof.Backend.Options;
using ForestProof.Backend.Services.Calculation.Interfaces;
using Scrutor;

var builder = WebApplication.CreateBuilder(args);

builder.AddOptions<CalculationOptions>();
builder.AddOptions<DataOptions>();
builder.AddOptions<GeometryOptions>();

builder.Services.Scan(selector => selector
    .FromAssemblyOf<ICarbonCalculator>()
    .AddClasses(classes => classes.Where(type =>
        type.Namespace is not null &&
        type.Namespace.StartsWith("ForestProof.Backend.Services", StringComparison.Ordinal)))
    .AsImplementedInterfaces()
    .WithSingletonLifetime());

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();