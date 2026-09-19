using ForestProof.Backend.Endpoints.Contracts;
using ForestProof.Backend.Persistence;
using ForestProof.Backend.Persistence.Entities;
using ForestProof.Backend.Services.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ForestProof.Backend.Endpoints;

/// <summary>
/// Регистрирует эндпоинты реестра территорий и проектов REST API.
/// </summary>
public static class RegistryEndpoints
{
    /// <summary>
    /// Добавляет группу эндпоинтов "/api/v1" для реестра территорий и проектов.
    /// </summary>
    /// <param name="endpoints">Построитель маршрутов приложения.</param>
    /// <returns>Тот же построитель маршрутов для цепочки вызовов.</returns>
    public static IEndpointRouteBuilder MapRegistryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1");

        group.MapGet("/areas", (IAoiCatalogReader aoiCatalogReader) =>
        {
            var areas = aoiCatalogReader.ReadAreas()
                .Select(aoi => new
                {
                    id = aoi.Id,
                    name = aoi.Name,
                    area_ha = aoi.AreaHectares,
                    selection_role = aoi.SelectionRole
                })
                .ToArray();

            return Results.Ok(areas);
        });

        group.MapGet("/projects", async (
            IDbContextFactory<ForestProofDbContext> contextFactory,
            CancellationToken cancellationToken) =>
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

            var projects = await dbContext.Projects
                .AsNoTracking()
                .OrderBy(project => project.Name)
                .Select(project => new ProjectResponse
                {
                    Id = project.Id,
                    Name = project.Name,
                    AoiId = project.AoiId,
                    ClaimedResult = project.ClaimedResult
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(projects);
        });

        group.MapPost("/projects", async (
            CreateProjectRequest request,
            IDbContextFactory<ForestProofDbContext> contextFactory,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new ApiError { Message = "Поле 'Name' обязательно." });

            await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

            var organizationId = await ResolveDefaultOrganizationAsync(dbContext, cancellationToken);

            var project = new Project
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = request.Name.Trim(),
                AoiId = string.IsNullOrWhiteSpace(request.AoiId) ? null : request.AoiId.Trim(),
                ClaimedResult = request.ClaimedResult
            };

            dbContext.Projects.Add(project);
            await dbContext.SaveChangesAsync(cancellationToken);

            var response = new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                AoiId = project.AoiId,
                ClaimedResult = project.ClaimedResult
            };

            return Results.Created($"/api/v1/projects/{project.Id}", response);
        });

        return endpoints;
    }

    private static async Task<Guid> ResolveDefaultOrganizationAsync(
        ForestProofDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations
            .OrderBy(item => item.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (organization is not null)
            return organization.Id;

        var defaultOrganization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Default"
        };

        dbContext.Organizations.Add(defaultOrganization);
        return defaultOrganization.Id;
    }
}
