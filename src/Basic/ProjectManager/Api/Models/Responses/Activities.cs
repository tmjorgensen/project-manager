using Api.Domain;

namespace Api.Models.Responses;

public record ActivityDto(Guid Id, string Name, ActivityStatus Status, string? AssignedTo, Guid ProjectId);

public record ActivityDetailsDto(Guid Id, string Name, string? Description, ActivityStatus Status, string? AssignedTo, Guid ProjectId);
