namespace Api.Domain;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Pending;
    public List<Activity> Activities { get; set; } = [];
}
