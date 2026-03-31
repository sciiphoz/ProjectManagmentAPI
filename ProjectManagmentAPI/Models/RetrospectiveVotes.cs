using ProjectManagementAPI.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementAPI.Models
{
    [Table("RetrospectiveVotes")]
    public class RetrospectiveVote
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid RetrospectiveItemId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public DateTime VotedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("RetrospectiveItemId")]
        public virtual RetrospectiveItem RetrospectiveItem { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}