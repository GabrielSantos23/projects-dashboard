using Microsoft.EntityFrameworkCore;
using ProjectDashboard.Shared.Models;

namespace ProjectDashboard.Shared.Data;

public class AppDbContext : DbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<ScanFolder> ScanFolders { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        
        modelBuilder.Entity<Project>()
            .Property(p => p.MetadataJson)
            .IsRequired()
            .HasDefaultValue("{}");

        modelBuilder.Entity<ScanFolder>()
            .HasIndex(f => f.Path)
            .IsUnique();
    }
}
