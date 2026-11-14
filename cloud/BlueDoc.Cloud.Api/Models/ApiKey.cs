using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueDoc.Cloud.Api.Models
{
    [Table("api_keys")]
    public class ApiKey
    {
        [Key]
        [Column("key_id")]
        public Guid KeyId { get; set; } = Guid.NewGuid();

        [Required]
        [Column("tenant_id")]
        public Guid TenantId { get; set; }

        [ForeignKey("TenantId")]
        public Tenant? Tenant { get; set; }

        [Required]
        [MaxLength(64)]
        [Column("key_hash")]
        public string KeyHash { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("label")]
        public string? Label { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_used_at")]
        public DateTime? LastUsedAt { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}

