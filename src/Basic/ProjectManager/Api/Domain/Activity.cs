namespace Api.Domain;

public class Activity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? AssignedTo { get; private set; }
    public ActivityStatus Status { get; private set; } = ActivityStatus.Pending;
    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;

    public void SetId(Guid id)
    {
        EnsureCanUpdate();
        if (id == Guid.Empty) throw new ArgumentException("Activity id cannot be empty.", nameof(id));
        Id = id;
    }

    public void SetName(string? name)
    {
        EnsureCanUpdate();
        name = name?.Trim();

        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Activity name cannot be empty.", nameof(name));
        Name = name;
    }

    public void SetDescription(string? description)
    {
        EnsureCanUpdate();
        description = description?.Trim();
        Description = description == string.Empty ? null : description;
    }

    public void SetAssignedTo(string? assignedTo)
    {
        EnsureCanUpdate();
        assignedTo = assignedTo?.Trim();

        if (assignedTo == string.Empty && Status == ActivityStatus.Active)
            throw new InvalidOperationException($"Activity must be assigned when status is {Status}.");

        AssignedTo = assignedTo == string.Empty ? null : assignedTo;
    }

    public void SetStatus(ActivityStatus status)
    {
        switch (status)
        {
            case ActivityStatus.Undefined:
                throw new ArgumentException($"Activity status has invalid value {status}.");

            case ActivityStatus.Active:
                {
                    if (AssignedTo is null)
                        throw new InvalidOperationException($"Activity assigned-to cannot be null or empty when status is {status}.");
                    break;
                }
        }

        Status = status;

    }

    public void SetProject(Project project)
    {
        EnsureCanUpdate();

        if (ProjectId != Guid.Empty && Project is null)
            throw new Exception("Initial project was not loaded.");

        Project.EnsureCanUpdate();

        if (project is null) throw new NullReferenceException("New project is missing.");

        project.EnsureCanUpdate();

        if (Status == ActivityStatus.Active && project.Status != ProjectStatus.Active)
            throw new InvalidOperationException($"Project must have status {ProjectStatus.Active} to accept an activity with status {Status}.");

        ProjectId = project.Id;
        Project = project;
    }

    private void EnsureCanUpdate()
    {
        if (Status == ActivityStatus.Closed)
            throw new InvalidOperationException($"Activity cannot be updated when it has status {ActivityStatus.Closed}.");
    }
}
