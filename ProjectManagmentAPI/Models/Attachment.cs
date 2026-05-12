// Models/Attachment.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjectManagementAPI.Models
{
    [Table("Attachments")]
    public class Attachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid? BacklogItemId { get; set; }

        public Guid? SubTaskId { get; set; }

        [Required]
        public Guid UploadedById { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        public long? FileSize { get; set; }

        [MaxLength(100)]
        public string? MimeType { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BacklogItemId")]
        public virtual BacklogItem? BacklogItem { get; set; }

        [ForeignKey("SubTaskId")]
        public virtual SubTask? SubTask { get; set; }

        [ForeignKey("UploadedById")]
        public virtual User UploadedBy { get; set; } = null!;
    }
}