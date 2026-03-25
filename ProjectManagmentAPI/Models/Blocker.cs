// Models/Blocker.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectManagementAPI.Enums;

namespace ProjectManagementAPI.Models
{
    [Table("Blockers")]
    public class Blocker
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid? BacklogItemId { get; set; }

        public Guid? SubTaskId { get; set; }

        [Required]
        public Guid ReportedById { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string Description { get; set; } = string.Empty;

        [Required]
        public BlockerSeverity Severity { get; set; } = BlockerSeverity.Medium;

        [Required]
        public BlockerStatus Status { get; set; } = BlockerStatus.Active;

        public DateTime? ResolvedAt { get; set; }

        [Column(TypeName = "text")]
        public string? ResolutionNote { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BacklogItemId")]
        public virtual BacklogItem? BacklogItem { get; set; }

        [ForeignKey("SubTaskId")]
        public virtual SubTask? SubTask { get; set; }

        [ForeignKey("ReportedById")]
        public virtual User ReportedBy { get; set; } = null!;
    }
}