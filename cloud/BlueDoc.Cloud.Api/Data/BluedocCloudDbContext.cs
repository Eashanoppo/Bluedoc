using Microsoft.EntityFrameworkCore;
using BlueDoc.Cloud.Api.Models;

namespace BlueDoc.Cloud.Api.Data
{
    public class BluedocCloudDbContext : DbContext
    {
        public BluedocCloudDbContext(DbContextOptions<BluedocCloudDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
        public DbSet<Machine> Machines => Set<Machine>();
        public DbSet<TelemetryRecord> TelemetryRecords => Set<TelemetryRecord>();
        public DbSet<Alert> Alerts => Set<Alert>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tenants
            modelBuilder.Entity<Tenant>()
                .HasIndex(t => t.Slug)
                .IsUnique();

            // ApiKeys
            modelBuilder.Entity<ApiKey>()
                .HasIndex(k => k.KeyHash)
                .IsUnique();

            modelBuilder.Entity<ApiKey>()
                .HasOne(k => k.Tenant)
                .WithMany(t => t.ApiKeys)
                .HasForeignKey(k => k.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Machines
            modelBuilder.Entity<Machine>()
                .HasIndex(m => new { m.TenantId, m.MachineGuid })
                .IsUnique();

            modelBuilder.Entity<Machine>()
                .HasIndex(m => m.TenantId);

            modelBuilder.Entity<Machine>()
                .HasIndex(m => new { m.TenantId, m.HealthStatus });

            modelBuilder.Entity<Machine>()
                .HasOne(m => m.Tenant)
                .WithMany(t => t.Machines)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // TelemetryRecords
            modelBuilder.Entity<TelemetryRecord>()
                .HasIndex(r => r.MachineId);

            modelBuilder.Entity<TelemetryRecord>()
                .HasIndex(r => r.ReceivedAt);

            modelBuilder.Entity<TelemetryRecord>()
                .HasOne(r => r.Machine)
                .WithMany(m => m.TelemetryRecords)
                .HasForeignKey(r => r.MachineId)
                .OnDelete(DeleteBehavior.Cascade);

            // Alerts
            modelBuilder.Entity<Alert>()
                .HasIndex(a => new { a.TenantId, a.Acknowledged, a.CreatedAt });

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Tenant)
                .WithMany(t => t.Alerts)
                .HasForeignKey(a => a.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Alert>()
                .HasOne(a => a.Machine)
                .WithMany(m => m.Alerts)
                .HasForeignKey(a => a.MachineId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

