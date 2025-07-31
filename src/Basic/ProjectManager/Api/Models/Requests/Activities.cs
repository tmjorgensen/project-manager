using Api.Domain;

namespace Api.Models.Requests;
public record SearchActivitiesDto(string? Text, ActivityStatus? Status, Guid? ProjectId, string? AssignedTo = null, int Skip = 0, int Take = 10, string Sort = "Id:asc");
public record CreateActivityDto(string Name, Guid ProjectId);
public record UpdateActivityDto(string? Name, string? Description, ActivityStatus? Status, Guid? ProjectId, string? AssignedTo);
