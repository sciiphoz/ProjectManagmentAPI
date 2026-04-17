// DTO/Responses/SprintResponses.cs
namespace ProjectManagementAPI.DTO.Responses
{
    public class SprintResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Goal { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Guid ProjectId { get; set; }
        public int TotalTasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
        public decimal? TotalStoryPoints { get; set; }
        public decimal? CompletedStoryPoints { get; set; }
        public double CompletionPercentage { get; set; }
        public int DaysRemaining { get; set; }

        // Дополнительные поля
        public int? CommittedStoryPoints { get; set; }
        public int? CompletedStoryPointsModel { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public string? RetrospectiveNotes { get; set; }
    }

    public class SprintBriefResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class SprintBoardResponse : SprintResponse
    {
        public List<BacklogItemBoardResponse> Tasks { get; set; } = new();
        public SprintMetrics Metrics { get; set; } = new();
        public List<BurndownPoint> BurndownData { get; set; } = new();
    }

    public class SprintMetrics
    {
        public decimal Velocity { get; set; }
        public List<BurndownPoint> BurndownData { get; set; } = new();
        public Dictionary<string, int> TasksByStatus { get; set; } = new();
        public Dictionary<string, int> TasksByAssignee { get; set; } = new();
    }

    public class BurndownPoint
    {
        public DateTime Date { get; set; }
        public decimal RemainingHours { get; set; }
        public decimal? IdealRemainingHours { get; set; }
        public int RemainingStoryPoints { get; set; }
    }

    public class SprintVelocityHistory
    {
        public Guid SprintId { get; set; }
        public string SprintName { get; set; } = string.Empty;
        public DateTime EndDate { get; set; }
        public decimal TotalStoryPoints { get; set; }
        public decimal CompletedStoryPoints { get; set; }
        public decimal Velocity { get; set; }
        public int CommittedTasks { get; set; }
        public int CompletedTasks { get; set; }
    }
}