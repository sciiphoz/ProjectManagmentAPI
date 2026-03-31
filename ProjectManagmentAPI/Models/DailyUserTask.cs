// Models/DailyUserTask.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementAPI.Models
{
    [Table("DailyUserTasks")]
    public class DailyUserTask
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? WorkedYesterday { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? PlanForToday { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Blockers { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}