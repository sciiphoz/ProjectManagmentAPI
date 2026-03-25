// Models/SprintVelocity.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementAPI.Models
{
    [Table("SprintVelocity")]
    public class SprintVelocity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid SprintId { get; set; }

        [Required]
        [Column(TypeName = "decimal(7,1)")]
        public decimal TotalStoryPoints { get; set; }

        [Required]
        [Column(TypeName = "decimal(7,1)")]
        public decimal CompletedStoryPoints { get; set; }

        [Required]
        public int CommittedTasksCount { get; set; }

        [Required]
        public int CompletedTasksCount { get; set; }

        [Required]
        [Column(TypeName = "decimal(7,1)")]
        public decimal Velocity { get; set; }

        [Required]
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SprintId")]
        public virtual Sprint Sprint { get; set; } = null!;
    }
}