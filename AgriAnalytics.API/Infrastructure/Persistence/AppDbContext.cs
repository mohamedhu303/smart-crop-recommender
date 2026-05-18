// AppDbContext.cs
// Entity Framework Core database context for AgriAnalytics.
// Configures the SQL Server connection and maps domain entities to database tables.

using AgriAnalytics.API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgriAnalytics.API.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Maps to the AgriDataRecords table in SQL Server
        public DbSet<AgriDataRecord> AgriDataRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AgriDataRecord>(entity =>
            {
                entity.ToTable("AgriDataRecords");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .ValueGeneratedOnAdd();

                entity.Property(e => e.Temperature)
                      .IsRequired()
                      .HasColumnType("real");

                entity.Property(e => e.Humidity)
                      .IsRequired()
                      .HasColumnType("real");

                entity.Property(e => e.Soil_pH)
                      .IsRequired()
                      .HasColumnType("real")
                      .HasColumnName("Soil_pH");

                entity.Property(e => e.Rainfall)
                      .IsRequired()
                      .HasColumnType("real");

                entity.Property(e => e.CropLabel)
                      .HasMaxLength(100)
                      .IsRequired(false);

                entity.Property(e => e.DateRecorded)
                      .IsRequired()
                      .HasColumnType("datetime2");

                // Index on DateRecorded for fast monthly aggregation queries
                entity.HasIndex(e => e.DateRecorded)
                      .HasDatabaseName("IX_AgriDataRecords_DateRecorded");

                // Index on CropLabel for filtering by crop type
                entity.HasIndex(e => e.CropLabel)
                      .HasDatabaseName("IX_AgriDataRecords_CropLabel");
            });
        }
    }
}