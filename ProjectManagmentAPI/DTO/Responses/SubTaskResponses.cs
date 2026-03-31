// DTO/Responses/SubTaskResponses.cs
namespace ProjectManagementAPI.DTO.Responses
{
    public class SubTaskResponse
    {
        public Guid Id { get; set; }
        public Guid BacklogItemId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }
        public string Status { get; set; } = string.Empty;
        public UserBriefResponse? Assignee { get; set; }
        public int OrderInParent { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsOverdue { get; set; }
        public bool HasBlockers { get; set; }
        public List<BlockerResponse> ActiveBlockers { get; set; } = new();

        // Дополнительные поля
        public int? ActualMinutes { get; set; }
        public double? Efficiency { get; set; }
    }

    public class SubTaskBriefResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public UserBriefResponse? Assignee { get; set; }
        public decimal? EstimatedHours { get; set; }
    }

    public class SubTaskStatisticsResponse
    {
        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public int InProgressCount { get; set; }
        public int TodoCount { get; set; }
        public decimal TotalEstimatedHours { get; set; }
        public decimal TotalActualHours { get; set; }
        public double CompletionPercentage { get; set; }
        public Dictionary<Guid, int> TasksByAssignee { get; set; } = new();
    }
}