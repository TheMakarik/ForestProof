namespace ForestProof.Backend.Extensions;

public static class OptionsExtensions
{
    public static IHostApplicationBuilder AddOptions<TOptions>(
        this IHostApplicationBuilder builder,
        string? sectionName = null)
        where TOptions : class
    {
        builder.Services
            .AddOptions<TOptions>()
            .Bind(builder.Configuration.GetSection(sectionName ?? typeof(TOptions).Name))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }
}
