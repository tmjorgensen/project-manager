namespace Api.Domain;

public class Activity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AssignedTo { get; set; }
    public ActivityStatus Status { get; set; } = ActivityStatus.Pending;
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}
