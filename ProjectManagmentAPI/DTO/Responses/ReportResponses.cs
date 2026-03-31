// DTO/Responses/ReportResponses.cs
namespace ProjectManagementAPI.DTO.Responses
{
    public class SprintReportResponse
    {
        public SprintResponse Sprint { get; set; } = null!;
        public List<BacklogItemReportItem> CompletedTasks { get; set; } = new();
        public List<BacklogItemReportItem> IncompleteTasks { get; set; } = new();
        public SprintMetrics Metrics { get; set; } = new();
        public string? ReviewNotes { get; set; }
        public string? RetrospectiveNotes { get; set; }
    }

    public class BacklogItemReportItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal? StoryPoints { get; set; }
        public decimal? EstimatedHours { get; set; }
        public decimal? ActualHours { get; set; }
        public UserBriefResponse? Assignee { get; set; }
        public DateTime? CompletedAt { get; set; }
        public List<SubTaskBriefResponse> SubTasks { get; set; } = new();
    }

    public class TeamPerformanceReportResponse
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<TeamMemberPerformance> TeamMembers { get; set; } = new();
        public TeamAggregateMetrics AggregateMetrics { get; set; } = new();
    }

    public class TeamMemberPerformance
    {
        public UserBriefResponse User { get; set; } = null!;
        public int TasksCompleted { get; set; }
        public int TasksInProgress { get; set; }
        public int TotalStoryPointsCompleted { get; set; }
        public decimal TotalEstimatedHours { get; set; }
        public decimal TotalActualHours { get; set; }
        public double Efficiency { get; set; }
        public double CompletionRate { get; set; }
        public List<BacklogItemReportItem> CompletedTasks { get; set; } = new();
    }

    public class TeamAggregateMetrics
    {
        public int TotalTasksCompleted { get; set; }
        public int TotalStoryPointsCompleted { get; set; }
        public decimal TotalEstimatedHours { get; set; }
        public decimal TotalActualHours { get; set; }
        public double OverallEfficiency { get; set; }
        public double AverageTasksPerMember { get; set; }
    }

    public class VelocityReportResponse
    {
        public List<SprintVelocityHistory> SprintHistory { get; set; } = new();
        public decimal AverageVelocity { get; set; }
        public decimal MedianVelocity { get; set; }
        public decimal MinVelocity { get; set; }
        public decimal MaxVelocity { get; set; }
        public double VelocityTrend { get; set; }
        public List<BurndownPoint> BurndownData { get; set; } = new();
    }
}