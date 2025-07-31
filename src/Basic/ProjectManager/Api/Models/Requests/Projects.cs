using Api.Domain;

namespace Api.Models.Requests;
public record SearchProjectsDto(string? Text, ProjectStatus? Status, int Skip = 0, int Take = 10, string Sort = "Id:asc");
public record CreateProjectDto(string Name);
public record UpdateProjectDto(string? Name, string? Description, ProjectStatus? Status);
