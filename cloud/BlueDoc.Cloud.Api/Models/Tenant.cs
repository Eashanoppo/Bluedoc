using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlueDoc.Cloud.Api.Models
{
    [Table("tenants")]
    public class Tenant
    {
        [Key]
        [Column("tenant_id")]
        public Guid TenantId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(200)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("slug")]
        public string Slug { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        [Column("plan")]
        public string Plan { get; set; } = "free";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
        public ICollection<Machine> Machines { get; set; } = new List<Machine>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}

