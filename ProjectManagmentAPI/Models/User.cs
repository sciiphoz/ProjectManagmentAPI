using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Models;

namespace ProjectManagementAPI.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public GlobalRole Role { get; set; } = GlobalRole.User;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsActive { get; set; } = true;

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmedAt { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        [InverseProperty("Owner")]
        public virtual ICollection<Project> OwnedProjects { get; set; } = new List<Project>();

        [InverseProperty("User")]
        public virtual ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();

        [InverseProperty("Assignee")]
        public virtual ICollection<BacklogItem> AssignedBacklogItems { get; set; } = new List<BacklogItem>();

        [InverseProperty("CreatedBy")]
        public virtual ICollection<BacklogItem> CreatedBacklogItems { get; set; } = new List<BacklogItem>();

        [InverseProperty("Assignee")]
        public virtual ICollection<SubTask> AssignedSubTasks { get; set; } = new List<SubTask>();

        [InverseProperty("User")]
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [InverseProperty("UploadedBy")]
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

        [InverseProperty("ReportedBy")]
        public virtual ICollection<Blocker> ReportedBlockers { get; set; } = new List<Blocker>();

        [InverseProperty("User")]
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

        [InverseProperty("User")]
        public virtual ICollection<DailyUserTask> DailyTasks { get; set; } = new List<DailyUserTask>();

        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiry { get; set; }
    }
}