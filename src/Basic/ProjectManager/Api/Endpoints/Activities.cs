using Api.Domain;
using Api.Extensions;
using Api.Models.Requests;
using Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints;

internal static class Activities
{
    public static async Task<IResult> SearchActivities([AsParameters] SearchActivitiesDto search, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(SearchActivities));

        var query = dbContext.Activities.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(act => act.Name.Contains(text) || (act.Description != null && act.Description.Contains(text)));
        }

        if (search.Status is not null)
            query = query.Where(act => act.Status == search.Status);

        if (search.ProjectId is not null)
            query = query.Where(act => act.ProjectId == search.ProjectId);

        if (search.AssignedTo is not null)
            query = query.Where(act => act.AssignedTo == search.AssignedTo);

        query = query.OrderBy(search.Sort)
            .Skip(search.Skip)
            .Take(search.Take);

        var resultQuery = query
            .Select(act => new ActivityDto(act.Id, act.Name, act.Status, act.AssignedTo, act.ProjectId));

        List<ActivityDto> result;
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

    public static async Task<IResult> CreateActivity([FromBody] CreateActivityDto body, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("CreateActivity");

        var activity = new Activity();
        dbContext.Activities.Add(activity);

        try
        {
            activity.SetId(Guid.NewGuid());
            activity.SetName(body.Name);

            var project = await dbContext.Projects.FirstOrDefaultAsync(pro => pro.Id == body.ProjectId, cancellationToken)
                ?? throw new NullReferenceException("Project was not found.");

            activity.SetProject(project);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Creating new activity failed.");

            return ex.ToErrorResult("Creating new activity failed.");
        }

        return Results.CreatedAtRoute(nameof(GetActivityById), new { activity.Id }, new CreatedDto(activity.Id));
    }

    public static async Task<IResult> GetActivityById([FromRoute] Guid id, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(GetActivityById));
        using var _ = logger.BeginScope(new { ActivityId = id });

        var query = dbContext.Activities
            .Where(act => act.Id == id)
            .Select(act => new ActivityDetailsDto(act.Id, act.Name, act.Description, act.Status, act.AssignedTo, act.ProjectId));

        object? activity;
        try
        {
            activity = await query.FirstOrDefaultAsync(cancellationToken)
                ?? throw new NullReferenceException("Activity was not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fetching activity failed.");

            return ex.ToErrorResult("Fetching activity for Id {id} failed.");
        }

        return Results.Ok(activity);
    }

    public static async Task<IResult> UpdateActivity([FromRoute] Guid id, [FromBody] UpdateActivityDto body, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(UpdateActivity));

        var query = dbContext.Activities
            .Where(act => act.Id == id)
            .Include(act => act.Project);

        Activity activity;
        try
        {
            activity = await query.FirstOrDefaultAsync(cancellationToken)
                ?? throw new NullReferenceException("Activity was not found.");

            if (body.Name is not null) activity.SetName(body.Name);
            if (body.Description is not null) activity.SetDescription(body.Description);
            if (body.AssignedTo is not null) activity.SetAssignedTo(body.AssignedTo);
            if (body.Status is not null) activity.SetStatus(body.Status.Value);

            if (body.ProjectId is not null)
            {
                var newProject = await dbContext.Projects.FirstOrDefaultAsync(pro => pro.Id == body.ProjectId.Value, cancellationToken)
                    ?? throw new NullReferenceException("New project was not found.");

                activity.SetProject(newProject);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Updating activity failed.");

            return ex.ToErrorResult("Updating activity failed.");
        }

        return Results.NoContent();
    }

    public static async Task<IResult> DeleteActivity([FromRoute] Guid id, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
    {

        var logger = loggerFactory.CreateLogger(nameof(DeleteActivity));

        dbContext.Activities.RemoveRange(dbContext.Activities.Where(x => x.Id == id));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deleting activity failed");

            return ex.ToErrorResult("Deleting activity failed.");
        }

        return Results.NoContent();
    }
}
