using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueDoc.Cloud.Api.Models
{
    [Table("telemetry_records")]
    public class TelemetryRecord
    {
        [Key]
        [Column("record_id")]
        public Guid RecordId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("machine_id")]
        public Guid MachineId { get; set; }

        [ForeignKey("MachineId")]
        public Machine? Machine { get; set; }

        [Required]
        [Column("tenant_id")]
        public Guid TenantId { get; set; }

        [Column("received_at")]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(20)]
        [Column("overall_status")]
        public string? OverallStatus { get; set; }

        [Column("total_tests")]
        public int TotalTests { get; set; }

        [Column("passed_tests")]
        public int PassedTests { get; set; }

        [Column("warning_tests")]
        public int WarningTests { get; set; }

        [Column("failed_tests")]
        public int FailedTests { get; set; }

        [Column("benchmark_score", TypeName = "numeric(5,1)")]
        public decimal? BenchmarkScore { get; set; }

        [MaxLength(50)]
        [Column("benchmark_rating")]
        public string? BenchmarkRating { get; set; }

        [Column("failed_summaries")]
        public string? FailedSummaries { get; set; }

        [Column("raw_payload", TypeName = "jsonb")]
        public string? RawPayload { get; set; }
    }
}

