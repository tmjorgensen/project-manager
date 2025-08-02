using Api.Domain;
using Api.Extensions;
using Api.Models.Requests;
using Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

internal static class Projects
{
    public static async Task<IResult> SearchProjects([AsParameters] SearchProjectsDto search, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(SearchProjects));

        var query = dbContext.Projects.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(pro => pro.Name.Contains(text) ||
                (pro.Description != null && pro.Description.Contains(text)));
        }

        if (search.Status is not null)
            query = query.Where(pro => pro.Status == search.Status);

        query = query
            .OrderBy(search.Sort)
            .Skip(search.Skip)
            .Take(search.Take);

        var resultQuery = query
            .Select(pro => new ProjectDto(pro.Id, pro.Name, pro.Status));

        List<ProjectDto> result;
        try
        {
            result = await resultQuery.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fetching projects by search failed.");

            return ex.ToErrorResult("Fetching projects by search failed.");
        }

        return Results.Ok(result);
    }

    public static async Task<IResult> CreateProject([FromBody] CreateProjectDto body, [FromServices] ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(CreateProject));

        var project = new Project();
        dbContext.Projects.Add(project);

        try
        {
            project.SetId(Guid.NewGuid());
            project.SetName(body.Name);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Creating new project failed.");

            return ex.ToErrorResult("Creating new project failed.");
        }

        return Results.CreatedAtRoute(nameof(GetProjectById), new { project.Id }, new CreatedDto(project.Id));
    }

    public static async Task<IResult> GetProjectById([FromRoute] Guid id, [FromServices] ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(GetProjectById));
        using var _ = logger.BeginScope(new { ProjectId = id });

        var query = dbContext.Projects
            .Where(pro => pro.Id == id)
            .Select(pro => new ProjectDetailsDto(pro.Id, pro.Name, pro.Description, pro.Status, 
                new(
                    pro.Activities.Count(act => act.Status == ActivityStatus.Pending),
                    pro.Activities.Count(act => act.Status == ActivityStatus.Active),
                    pro.Activities.Count(act => act.Status == ActivityStatus.Closed)
                )));

        ProjectDetailsDto? result;
        try
        {
            result = await query.FirstOrDefaultAsync(cancellationToken)
                ?? throw new NullReferenceException("Project was not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fetching project failed.");

            return ex.ToErrorResult("Fetching project failed.");
        }

        return Results.Ok(result);
    }

    public static async Task<IResult> UpdateProject([FromRoute] Guid id, [FromBody] UpdateProjectDto body, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(UpdateProject));
        using var _ = logger.BeginScope(new { ProjectId = id });

        var query = dbContext.Projects
            .Where(pro => pro.Id == id)
            .Include(x => x.Activities);

        try
        {
            var project = await query.FirstOrDefaultAsync(cancellationToken)
                ?? throw new NullReferenceException("Project was not found.");

            if (body.Name is not null) project.SetName(body.Name);
            if (body.Description is not null) project.SetDescription(body.Description);

            if (body.Status is not null)
                project.SetStatus(body.Status.Value);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Updating project failed.");

            return ex.ToErrorResult("Updating project failed.");
        }

        return Results.NoContent();
    }

    public static async Task<IResult> DeleteProject([FromRoute] Guid id, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(DeleteProject));
        using var _ = logger.BeginScope(new { ProjectId = id });

        dbContext.Projects.RemoveRange(dbContext.Projects.Where(x => x.Id == id));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deleting project failed.");

            return ex.ToErrorResult("Deleting project failed.");
        }

        return Results.NoContent();
    }
}

