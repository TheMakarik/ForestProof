using ForestProof.Backend.Domain.Analysis;
using ForestProof.Backend.Endpoints.Contracts;
using ForestProof.Backend.Services.Analysis.Interfaces;

namespace ForestProof.Backend.Endpoints;

/// <summary>
/// Регистрирует эндпоинты статуса расчётов REST API.
/// </summary>
public static class StatusEndpoints
{
    /// <summary>
    /// Добавляет группу эндпоинтов "/api/v1" для запроса статуса расчётов.
    /// </summary>
    /// <param name="endpoints">Построитель маршрутов приложения.</param>
    /// <returns>Тот же построитель маршрутов для цепочки вызовов.</returns>
    public static IEndpointRouteBuilder MapStatusEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1");

        group.MapGet("/analyses/{aoiId}/status", (
            string aoiId,
            int startYear,
            int endYear,
            IAnalysisPipeline pipeline) =>
        {
            var request = new AnalysisRequest
            {
                AoiId = aoiId,
                StartYear = startYear,
                EndYear = endYear
            };

            try
            {
                var summary = pipeline.Run(request);
                return Results.Ok(ToResponse(summary, aoiId));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException)
            {
                return Results.BadRequest(new ApiError { Message = exception.Message });
            }
        });

        return endpoints;
    }

    private static AnalysisStatusResponse ToResponse(AnalysisSummary summary, string aoiId) => new()
    {
        RunId = summary.RunId,
        Status = summary.Status.ToString(),
        MethodVersion = summary.MethodVersion,
        DataVersion = summary.DataVersion,
        CreatedAt = summary.CreatedAt,
        InputHash = summary.InputHash,
        StartYear = summary.StartYear,
        EndYear = summary.EndYear,
        Warnings = summary.Warnings,
        Progress = 1.0,
        LogReference = string.IsNullOrWhiteSpace(aoiId)
            ? null
            : $"/api/v1/analyses/{aoiId}/changes?startYear={summary.StartYear}&endYear={summary.EndYear}"
    };
}
