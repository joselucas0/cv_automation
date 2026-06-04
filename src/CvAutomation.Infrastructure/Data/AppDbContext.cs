using CvAutomation.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CvAutomation.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ResumeBlock> ResumeBlocks => Set<ResumeBlock>();
    public DbSet<GeneratedResume> GeneratedResumes => Set<GeneratedResume>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ResumeBlock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(30);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Company).HasMaxLength(100);
            entity.Property(e => e.Seniority).HasMaxLength(20);
            entity.Property(e => e.StackContext).HasMaxLength(100);
            entity.Property(e => e.CompanyKey).HasMaxLength(50);
            entity.Property(e => e.JuniorSpecialties).HasMaxLength(1000);
        });

        modelBuilder.Entity<GeneratedResume>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CompanyName).IsRequired().HasMaxLength(100);
        });
    }
}
