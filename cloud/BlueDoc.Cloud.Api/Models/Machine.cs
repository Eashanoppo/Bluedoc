using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueDoc.Cloud.Api.Models
{
    [Table("machines")]
    public class Machine
    {
        [Key]
        [Column("machine_id")]
        public Guid MachineId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("tenant_id")]
        public Guid TenantId { get; set; }

        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("machine_guid")]
        public string MachineGuid { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Column("hostname")]
        public string Hostname { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("department")]
        public string? Department { get; set; }

        [MaxLength(200)]
        [Column("os_name")]
        public string? OsName { get; set; }

        [MaxLength(50)]
        [Column("os_build")]
        public string? OsBuild { get; set; }

        [MaxLength(10)]
        [Column("architecture")]
        public string? Architecture { get; set; }

        [MaxLength(200)]
        [Column("adapter_name")]
        public string? AdapterName { get; set; }

        [MaxLength(200)]
        [Column("manufacturer")]
        public string? Manufacturer { get; set; }

        [MaxLength(300)]
        [Column("hardware_id")]
        public string? HardwareId { get; set; }

        [MaxLength(100)]
        [Column("driver_version")]
        public string? DriverVersion { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("health_status")]
        public string HealthStatus { get; set; } = "Unknown";

        [Column("benchmark_score", TypeName = "numeric(5,1)")]
        public decimal? BenchmarkScore { get; set; }

        [Column("is_compliant")]
        public bool IsCompliant { get; set; } = true;

        [MaxLength(500)]
        [Column("compliance_issue")]
        public string? ComplianceIssue { get; set; }

        [Column("last_heartbeat")]
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;

        [Column("enrolled_at")]
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        public ICollection<TelemetryRecord> TelemetryRecords { get; set; } = new List<TelemetryRecord>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}

