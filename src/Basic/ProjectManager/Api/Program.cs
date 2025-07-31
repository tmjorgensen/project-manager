using Api.Domain;
using Api.Models.Requests;
using Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddLogging(logging => logging.AddConsole());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/projects", 
    static async ([AsParameters]SearchProjectsDto search, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("SearchProjects");

        var query = dbContext.Projects.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(pro => pro.Name.Contains(text) || 
                (pro.Description != null && pro.Description.Contains(text)));
        }

        if (search.Status is not null)
            query = query.Where(pro => pro.Status == search.Status);

        query = orderQueryable(query, search.Sort)
            .Skip(search.Skip)
            .Take(search.Take);

        var resultQuery = query
            .Select(pro => new { pro.Id, pro.Name, pro.Description, pro.Status });

        object result;
        try
        {
            result = await resultQuery.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fetching projects for search failed.");
            return Results.InternalServerError("Fetching projects for search failed.");
        }

        return Results.Ok(result);
    });
app.MapPost("/projects", 
    static async ([FromBody] CreateProjectDto body, [FromServices] ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("CreateProject");

        if (string.IsNullOrWhiteSpace(body.Name))
            return Results.BadRequest("Name cannot be empty.");

        var id = Guid.NewGuid();
        dbContext.Projects.Add(new() { Id = id, Name = body.Name.Trim(), Status = ProjectStatus.Pending });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Saving new project failed.");
            return Results.InternalServerError($"Saving new project failed.");
        }

        return Results.CreatedAtRoute("ProjectById", new { id }, new CreatedDto(id));
    });
app.MapGet("/projects/{id:guid}", 
    static async ([FromRoute]Guid id, [FromServices] ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("GetProjectById");

        var query = dbContext.Projects
            .Where(pro => pro.Id == id)
            .Select(pro => new
            {
                pro.Id,
                pro.Name,
                pro.Description,
                pro.Status,
                pendingActivities = pro.Activities.Count(act => act.Status == ActivityStatus.Pending),
                ActiveActivities = pro.Activities.Count(act => act.Status == ActivityStatus.Active),
                ClosedActivities = pro.Activities.Count(act => act.Status == ActivityStatus.Closed)
            });

        object? result;
        try
        {
            result = await query.FirstOrDefaultAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Fetching project for Id {ProjectId} failed.", id);
            return Results.InternalServerError($"Fetching project for Id {id} failed.");
        }

        if (result is null) 
            return Results.NotFound($"Project with Id {id} was not found.");

        return Results.Ok(result);
    }).WithName("ProjectById");
app.MapPut("/projects/{id:guid}", 
    static async ([FromRoute] Guid id, [FromBody]UpdateProjectDto body, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("UpdateProject");

        var query = dbContext.Projects
            .Where(pro => pro.Id == id)
            .Include(x => x.Activities);

        Project? project;
        try
        {
            project = await query.FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fetching project for Id {ProjectId} failed.", id);
            return Results.InternalServerError($"Fetching project for Id {id} failed.");
        }

        if (project is null) 
            return Results.NotFound($"Project for Id {id} was not found.");

        if (project.Status == ProjectStatus.Closed)
            return Results.Conflict($"Project cannot be changed when it has status {ProjectStatus.Closed}.");

        var name = body.Name?.Trim();
        if (name is not null && project.Name != name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Results.BadRequest($"Name cannot be empty.");

            project.Name = name;
        }

        var description = body.Description?.Trim();
        if (description is not null && project.Description != description)
            project.Description = string.IsNullOrWhiteSpace(description) ? null : description;

        if (body.Status is not null && project.Status != body.Status)
        {
            switch (body.Status)
            {
                case ProjectStatus.Undefined:
                    return Results.BadRequest($"Status has invalid value {body.Status}.");

                case ProjectStatus.Pending:
                    {
                        var count = project.Activities.Count(act => act.Status != ActivityStatus.Pending);
                        if (count > 0)
                            return Results.Conflict($"Status cannot be changed to {ProjectStatus.Pending} because project has {count} activities with status other that {ActivityStatus.Pending}.");
                        break;
                    }

                case ProjectStatus.Closed:
                    {
                        var count = project.Activities.Count(act => act.Status != ActivityStatus.Closed);
                        if (count > 0)
                            return Results.Conflict($"Status cannot be changed to {ProjectStatus.Closed} because project has {count} activities with status other that {ActivityStatus.Closed}.");
                        break;
                    }
            }

            project.Status = body.Status.Value;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Updating project for Id {ProjectId} failed.", id);
            return Results.InternalServerError($"Updating project for Id {id} failed.");
        }

        return Results.NoContent();
    });
app.MapDelete("/projects/{id:guid}", 
    static async ([FromRoute] Guid id, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("DeleteProject");

        dbContext.Projects.RemoveRange(dbContext.Projects.Where(x => x.Id == id));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deleting project for Id {ProjectId} failed.", id);
            return Results.InternalServerError($"Deleting project for Id {id} failed.");
        }

        return Results.NoContent();
    });

app.MapGet("/activities", 
    static async ([AsParameters]SearchActivitiesDto search, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("SearchActivities");

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

        query = orderQueryable(query, search.Sort)
            .Skip(search.Skip)
            .Take(search.Take);

        var resultQuery = query
            .Select(act => new { act.Id, act.Name, act.Description, act.Status, act.AssignedTo, act.ProjectId });

        object result;
        try
        {
            result = await resultQuery.ToListAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Fetching projects for search failed.");
            return Results.InternalServerError($"Fetching projects for search failed.");

        }

        return Results.Ok(result);
    });
app.MapPost("/activities", 
    static async ([FromBody]CreateActivityDto body, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("CreateActivity");

        if (body.ProjectId == Guid.Empty)
            return Results.BadRequest($"{nameof(CreateActivityDto.ProjectId)} cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(body.Name))
            return Results.BadRequest($"{nameof(CreateActivityDto.Name)} cannot be null or empty.");

        var proQuery = dbContext.Projects
            .Where(pro => pro.Id == body.ProjectId)
            .Select(pro => new Project { Status = pro.Status });

        Project? project;
        try
        {
            project = await proQuery.FirstOrDefaultAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Locating project with id {ProjectId} failed.", body.ProjectId);
            return Results.InternalServerError($"Locating project with id {body.ProjectId} failed.");
        }

        if (project is null)
            return Results.BadRequest($"Project Id {body.ProjectId} is not valid.");

        if (!new List<ProjectStatus> { ProjectStatus.Pending, ProjectStatus.Active }.Contains(project.Status))
            return Results.Conflict($"Project for Id {body.ProjectId} does not have status that allows creating activities.");

        var id = Guid.NewGuid();
        dbContext.Activities.Add(new() 
        { 
            Id = id, 
            Name = body.Name.Trim(), 
            ProjectId = body.ProjectId, 
            Status = ActivityStatus.Pending 
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Saving new activity failed.");
            return Results.InternalServerError($"Saving new activity failed.");
        }

        return Results.CreatedAtRoute("ActivityById", new { id }, new CreatedDto(id));
    });
app.MapGet("/activities/{id:guid}", 
    static async ([FromRoute] Guid id, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("GetActivityById");

        var query = dbContext.Activities
            .Where(act => act.Id == id)
            .Select(act => new { act.Id, act.Name, act.Description, act.Status, act.AssignedTo, act.ProjectId });

        object? activity;
        try
        {
            activity = await query.FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fetching activity for Id {ActivityId} failed.", id);
            return Results.InternalServerError($"Fetching activity for Id {id} failed.");
        }

        if (activity is null) return Results.NotFound($"Activity with Id {id} was not found.");

        return Results.Ok(activity);
    }).WithName("ActivityById");
app.MapPut("/activities/{id:guid}", 
    static async ([FromRoute] Guid id, [FromBody]UpdateActivityDto body, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("UpdateActivity");

        var query = dbContext.Activities
            .Where(act => act.Id == id)
            .Include(act => act.Project);

        Activity? activity;
        try
        {
            activity = await query.FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
        logger.LogError(ex, "Fetching activity for Id {ActivityId} failed.", id);
            return Results.InternalServerError($"Fetching activity for Id {id} failed.");
        }

        if (activity is null) 
            return Results.NotFound($"Activity for Id {id} was not found.");


        if (activity.Status == ActivityStatus.Closed)
            return Results.Conflict($"Activity cannot be changed when it has status {ActivityStatus.Closed}.");

        var name = body.Name?.Trim();
        if (name is not null && activity.Name != name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Results.BadRequest($"Activity name cannot be null or empty.");

            activity.Name = name;
        }

        var description = body.Description?.Trim();
        if (description is not null && activity.Description != description)
            activity.Description = description;

        var assignedTo = body.AssignedTo?.Trim();
        if (assignedTo is not null && activity.AssignedTo != assignedTo)
        {
            if (activity.Status == ActivityStatus.Active && string.IsNullOrWhiteSpace(assignedTo))
                return Results.Conflict($"Activity assigned-to cannot be null or empty when status is {ActivityStatus.Active}.");

            activity.AssignedTo = string.IsNullOrWhiteSpace(assignedTo) ? null : assignedTo;
        }

        if (body.Status  is not null && activity.Status != body.Status)
        {
            switch (body.Status)
            {
                case ActivityStatus.Undefined:
                    return Results.BadRequest($"Activity status has invalid value {body.Status}.");

                case ActivityStatus.Active:
                    {
                        if (activity.AssignedTo is null)
                            return Results.Conflict($"Activity assigned-to cannot be null or empty when status is {ActivityStatus.Active}.");
                        break;
                    }
            }

            activity.Status = body.Status.Value;
        }

        if (body.ProjectId is not null && activity.ProjectId != body.ProjectId)
        {
            var proQuery = dbContext.Projects
                .Where(pro => pro.Id == body.ProjectId)
                .Select(pro => new Project { Status = pro.Status });

            Project? newProject;
            try
            {
                newProject = await proQuery.FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Locating new project with id {ProjectId} failed.", body.ProjectId);
                return Results.InternalServerError($"Locating new project with id {body.ProjectId} failed.");
            }

            if (newProject is null)
                return Results.BadRequest($"Project Id {body.ProjectId} is not valid.");

            if (!new List<ProjectStatus> { ProjectStatus.Pending, ProjectStatus.Active }.Contains(newProject.Status))
                return Results.Conflict($"Project for Id {body.ProjectId} does not have status that allows creating activities.");

            if (activity.Status == ActivityStatus.Active && newProject.Status != ProjectStatus.Active)
                return Results.Conflict($"Project for Id {body.ProjectId} must have status {ProjectStatus.Active} to accept an activity of status {ActivityStatus.Active}.");

            activity.ProjectId = newProject.Id;
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Updating activity for Id {ActivityId} failed.", id);
            return Results.InternalServerError($"Updating activity for Id {id} failed.");
        }

        return Results.NoContent();
    });
app.MapDelete("/activities/{id:guid}", 
    static async ([FromRoute] Guid id, ApplicationDbContext dbContext, ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("DeleteActivity");

        dbContext.Activities.RemoveRange(dbContext.Activities.Where(x => x.Id == id));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Deleting activity for Id {ActivityId} failed", id);
            return Results.InternalServerError($"Deleting activity for Id {id} failed.");
        }

        return Results.NoContent();
    });

app.Run();

static IQueryable<T> orderQueryable<T>(IQueryable<T> queryable, string querySort)
{
    if (string.IsNullOrWhiteSpace(querySort))
        return queryable; // Return the original queryable if no sort is specified  

    var sortparts = querySort.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    if (sortparts.Length == 2)
    {
        var property = sortparts[0];
        var direction = sortparts[1].ToLower();

        if (direction == "asc")
            return queryable.OrderBy(x => EF.Property<T>(x, property));
        else if (direction == "desc")
            return queryable.OrderByDescending(x => EF.Property<T>(x, property));
    }

    return queryable; // Default return if sortparts are invalid  
}
