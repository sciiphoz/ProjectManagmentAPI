// Models/Sprint.cs
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementAPI.Models
{
    [Table("Sprints")]
    public class Sprint
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Goal { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime EndDate { get; set; }

        [Required]
        public bool IsActive { get; set; } = false;

        [Required]
        public SprintStatus Status { get; set; } = SprintStatus.Planned;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CommittedStoryPoints { get; set; }      
        public int? CompletedStoryPoints { get; set; }      
        public DateTime? CompletedAt { get; set; }          
        [Column(TypeName = "text")]
        public string? ReviewNotes { get; set; }            
        [Column(TypeName = "text")]
        public string? RetrospectiveNotes { get; set; }

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [InverseProperty("Sprint")]
        public virtual ICollection<BacklogItem> BacklogItems { get; set; } = new List<BacklogItem>();

        [InverseProperty("Sprint")]
        public virtual SprintVelocity? Velocity { get; set; }
    }
}