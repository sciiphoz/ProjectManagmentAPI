using ProjectManagementAPI.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementAPI.Models
{
    [Table("RetrospectiveItems")]
    public class RetrospectiveItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid SprintId { get; set; }

        [Required]
        public Guid CreatedById { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // Good, Bad, Idea, Action

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public int VoteCount { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("SprintId")]
        public virtual Sprint Sprint { get; set; } = null!;

        [ForeignKey("CreatedById")]
        public virtual User CreatedBy { get; set; } = null!;

        [InverseProperty("RetrospectiveItem")]
        public virtual ICollection<RetrospectiveVote> Votes { get; set; } = new List<RetrospectiveVote>();
    }
}