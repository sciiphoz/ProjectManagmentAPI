using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProjectManagementAPI.Enums;

namespace ProjectManagementAPI.Models
{
    [Table("ProjectInvitations")]
    public class ProjectInvitation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public ProjectRole InvitedRole { get; set; }

        [Required]
        public Guid InvitedByUserId { get; set; }

        [Required]
        [MaxLength(128)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsAccepted { get; set; } = false;

        public DateTime? AcceptedAt { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; } = null!;

        [ForeignKey("InvitedByUserId")]
        public virtual User InvitedByUser { get; set; } = null!;
    }
}