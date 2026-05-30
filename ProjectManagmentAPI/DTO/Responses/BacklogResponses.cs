// DTO/Responses/BacklogResponses.cs
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.DTO.Responses
{
    public class BacklogItemResponse
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public int Priority { get; set; }
        public decimal? StoryPoints { get; set; }
        public decimal? EstimatedHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public UserBriefResponse? Assignee { get; set; }
        public UserBriefResponse CreatedBy { get; set; } = null!;
        public SprintBriefResponse? Sprint { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int SubTasksCount { get; set; }
        public int CompletedSubTasksCount { get; set; }
        public int CommentsCount { get; set; }
        public int AttachmentsCount { get; set; }
        public List<BlockerResponse> ActiveBlockers { get; set; } = new();

        // Дополнительные поля
        public int? SprintPriority { get; set; }
        public DateTime? StartedAt { get; set; }
        public int? ActualHours { get; set; }
        public double? Efficiency { get; set; }
    }

    public class BacklogItemBoardResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Priority { get; set; }
        public decimal? StoryPoints { get; set; }
        public decimal? EstimatedHours { get; set; }
        public List<BlockerResponse> ActiveBlockers { get; set; } = new();
        public UserBriefResponse? Assignee { get; set; }
        public bool HasBlockers { get; set; }
        public int SubTasksCount { get; set; }
        public int CompletedSubTasksCount { get; set; }
        public string? ColorTag { get; set; }
    }

    public class BacklogItemDetailResponse : BacklogItemResponse
    {
        public List<SubTaskResponse> SubTasks { get; set; } = new();
        public List<CommentResponse> Comments { get; set; } = new();
        public List<AttachmentResponse> Attachments { get; set; } = new();
        public List<ActivityLogResponse> ActivityHistory { get; set; } = new();
    }

    public class CommentResponse
    {
        public Guid Id { get; set; }
        public Guid BacklogItemId { get; set; }
        public string Content { get; set; } = string.Empty;
        public UserBriefResponse User { get; set; } = null!;
        public List<UserBriefResponse> MentionedUsers { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
    }

    public class AttachmentResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? FileSizeFormatted => FileSize.HasValue ? FormatFileSize(FileSize.Value) : null;
        public string? MimeType { get; set; }
        public UserBriefResponse UploadedBy { get; set; } = null!;
        public DateTime UploadedAt { get; set; }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }

    public class BlockerResponse
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public UserBriefResponse ReportedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolutionNote { get; set; }
    }
}