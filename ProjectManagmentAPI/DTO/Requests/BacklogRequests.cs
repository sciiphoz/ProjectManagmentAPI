// DTO/Requests/BacklogRequests.cs
using ProjectManagementAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementAPI.DTO.Requests
{
    public class CreateBacklogItemRequest
    {
        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public BacklogItemType Type { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(5000)]
        public string? Description { get; set; }

        [MaxLength(5000)]
        public string? AcceptanceCriteria { get; set; }

        public int? Priority { get; set; }

        [Range(0, 100)]
        public decimal? StoryPoints { get; set; }

        [Range(0, 999)]
        public decimal? EstimatedHours { get; set; }

        public Guid? AssigneeId { get; set; }

        // Для сервиса
        public Guid CreatedById { get; set; }
    }

    public class UpdateBacklogItemRequest
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(5000)]
        public string? Description { get; set; }

        [MaxLength(5000)]
        public string? AcceptanceCriteria { get; set; }

        public BacklogItemType? Type { get; set; }
        public int? Priority { get; set; }
        public decimal? StoryPoints { get; set; }
        public decimal? EstimatedHours { get; set; }
        public BacklogItemStatus? Status { get; set; }
        public Guid? AssigneeId { get; set; }

        // Для сервиса
        public Guid UserId { get; set; }
    }

    public class ChangeTaskStatusRequest
    {
        [Required]
        public BacklogItemStatus NewStatus { get; set; }

        public string? Comment { get; set; }

        // Для сервиса
        public Guid UserId { get; set; }
    }

    public class ReorderBacklogRequest
    {
        [Required]
        public List<ReorderItem> Items { get; set; } = new();
    }

    public class ReorderItem
    {
        public Guid Id { get; set; }
        public int NewOrder { get; set; }
    }

    public class AddCommentRequest
    {
        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = string.Empty;

        public List<Guid>? MentionedUserIds { get; set; }

        // Для сервиса
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserLogin { get; set; } = string.Empty;
    }

    public class UpdateCommentRequest
    {
        [Required]
        [MaxLength(10000)]
        public string Content { get; set; } = string.Empty;
    }

    public class UploadAttachmentRequest
    {
        [Required]
        public byte[] FileContent { get; set; } = Array.Empty<byte>();

        [Required]
        public string FileName { get; set; } = string.Empty;

        public string? MimeType { get; set; }

        // Для сервиса
        public Guid UploadedById { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
    }
}