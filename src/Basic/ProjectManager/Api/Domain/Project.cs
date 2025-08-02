namespace Api.Domain;

public class Project
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; } = ProjectStatus.Pending;
    public List<Activity> Activities { get; private set; } = [];

    public void SetId(Guid id)
    {
        EnsureCanUpdate();
        if (id == Guid.Empty) throw new ArgumentException("Project id cannot be empty.", nameof(id));
        Id = id;
    }

    public void SetName(string? name)
    {
        EnsureCanUpdate();
        name = name?.Trim();

        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Project name cannot be empty.", nameof(name));
        Name = name;
    }

    public void SetDescription(string? description)
    {
        EnsureCanUpdate();
        Description = description?.Trim();
    }

    public void SetStatus(ProjectStatus status)
    {
        EnsureCanUpdate();

        switch (status)
        {
            case ProjectStatus.Undefined:
                throw new ArgumentException($"Project status cannot be {status}.");

            case ProjectStatus.Pending:
                {
                    var count = Activities.Count(act => act.Status != ActivityStatus.Pending);
                    if (count > 0)
                        throw new InvalidOperationException($"Project status cannot be changed to {status} because project has {count} activities with status other that {ActivityStatus.Pending}.");
                    break;
                }

            case ProjectStatus.Closed:
                {
                    var count = Activities.Count(act => act.Status != ActivityStatus.Closed);
                    if (count > 0)
                        throw new InvalidOperationException($"Project status cannot be changed to {status} because project has {count} activities with status other that {ActivityStatus.Closed}.");
                    break;
                }
        }

        Status = status;
    }

    public void AddActivity(Activity activity)
    {
        // if this is the place to do it?
        throw new NotImplementedException();
    }

    public void RemoveActivity(Activity ectivity)
    {
        // if this is the place to do it?
        throw new NotImplementedException();
    }

    public void EnsureCanUpdate()
    {
        if (Status == ProjectStatus.Closed) 
            throw new InvalidOperationException($"Project cannot be updated when it has status {ProjectStatus.Closed}.");
    }
}
