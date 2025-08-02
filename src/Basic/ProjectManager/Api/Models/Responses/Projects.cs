using Api.Domain;

namespace Api.Models.Responses;

public record ProjectDto(Guid Id, string Name, ProjectStatus Status);

public record ProjectDetailsDto(Guid Id, string Name, string? Description, ProjectStatus Status, ProjectDetailsDto.ActivitiesDetails Activities)
{
    public record ActivitiesDetails(int Pending, int Active, int Closed);
}