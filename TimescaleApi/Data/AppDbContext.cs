using Microsoft.EntityFrameworkCore;
using TimescaleApi.Entities;

namespace TimescaleApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<MeasurementValue> Values { get; set; }

    public DbSet<ProcessingResult> Results { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MeasurementValue>(entity =>
        {
            entity.ToTable("Values");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(x => x.FileName);

            entity.HasIndex(x => new { x.FileName, x.Date });
        });

        modelBuilder.Entity<ProcessingResult>(entity =>
        {
            entity.ToTable("Results");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(x => x.FileName)
                .IsUnique();

            entity.HasIndex(x => x.FirstOperationDate);

            entity.HasIndex(x => x.AverageValue);

            entity.HasIndex(x => x.AverageExecutionTime);
        });
    }
}