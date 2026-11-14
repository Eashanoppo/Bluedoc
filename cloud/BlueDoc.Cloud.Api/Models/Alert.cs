using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueDoc.Cloud.Api.Models
{
    [Table("alerts")]
    public class Alert
    {
        [Key]
        [Column("alert_id")]
        public Guid AlertId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("tenant_id")]
        public Guid TenantId { get; set; }

        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [Column("machine_id")]
        public Guid? MachineId { get; set; }

        [ForeignKey("MachineId")]
        public Machine? Machine { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("severity")]
        public string Severity { get; set; } = "Info"; // Critical | Warning | Info

        [MaxLength(100)]
        [Column("rule_id")]
        public string? RuleId { get; set; }

        [Required]
        [MaxLength(500)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("detail")]
        public string? Detail { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("acknowledged")]
        public bool Acknowledged { get; set; } = false;

        [Column("ack_at")]
        public DateTime? AckAt { get; set; }
    }
}

