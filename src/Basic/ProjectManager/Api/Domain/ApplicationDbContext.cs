using Microsoft.EntityFrameworkCore;

namespace Api.Domain;

public class ApplicationDbContext : DbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Activity> Activities { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasMany(p => p.Activities)
            .WithOne(t => t.Project)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
