// Models/SubTask.cs
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementAPI.Models
{
    [Table("SubTasks")]
    public class SubTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid BacklogItemId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal? EstimatedHours { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal? ActualHours { get; set; }

        [Required]
        public SubTaskStatus Status { get; set; } = SubTaskStatus.ToDo;

        public Guid? AssigneeId { get; set; }

        [Required]
        public int OrderInParent { get; set; } = 0;

        public DateTime? StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("BacklogItemId")]
        public virtual BacklogItem BacklogItem { get; set; } = null!;

        [ForeignKey("AssigneeId")]
        public virtual User? Assignee { get; set; }

        [InverseProperty("SubTask")]
        public virtual ICollection<Blocker> Blockers { get; set; } = new List<Blocker>();

        [InverseProperty("SubTask")]
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}