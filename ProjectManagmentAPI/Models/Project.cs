// Models/Project.cs
using ProjectManagementAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementAPI.Models
{
    [Table("Projects")]
    public class Project
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Description { get; set; }

        [Required]
        public Guid OwnerId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public bool IsArchived { get; set; } = false;

        [ForeignKey("OwnerId")]
        public virtual User Owner { get; set; } = null!;

        [InverseProperty("Project")]
        public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

        [InverseProperty("Project")]
        public virtual ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();

        [InverseProperty("Project")]
        public virtual ICollection<BacklogItem> BacklogItems { get; set; } = new List<BacklogItem>();

        [InverseProperty("Project")]
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
    }
}