using ProjectManagementAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementAPI.Models
{
    [Table("BacklogItems")]
    public class BacklogItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        public Guid? SprintId { get; set; }

        [Required]
        public BacklogItemType Type { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "text")]
        public string? Description { get; set; }

        [Column(TypeName = "text")]
        public string? AcceptanceCriteria { get; set; }

        [Required]
        public int Priority { get; set; } = 0;

        [Column(TypeName = "decimal(5,1)")]
        public decimal? StoryPoints { get; set; }

        [Column(TypeName = "decimal(6,2)")]
        public decimal? EstimatedHours { get; set; }

        [Required]
        public BacklogItemStatus Status { get; set; } = BacklogItemStatus.Backlog;

        public Guid? AssigneeId { get; set; }

        [Required]
        public Guid CreatedById { get; set; }

        [Required]
        public int OrderInBacklog { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int? SprintPriority { get; set; }             
        public DateTime? StartedAt { get; set; }             
        public int? ActualHours { get; set; }

        // Navigation properties
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [ForeignKey("SprintId")]
        public virtual Sprint? Sprint { get; set; }

        [ForeignKey("AssigneeId")]
        public virtual User? Assignee { get; set; }

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; } = null!;

        [InverseProperty("BacklogItem")]
        public virtual ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();

        [InverseProperty("BacklogItem")]
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [InverseProperty("BacklogItem")]
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

        [InverseProperty("BacklogItem")]
        public virtual ICollection<Blocker> Blockers { get; set; } = new List<Blocker>();
    }
}