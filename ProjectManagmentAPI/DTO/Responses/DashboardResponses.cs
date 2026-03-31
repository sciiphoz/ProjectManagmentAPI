// DTO/Responses/DashboardResponses.cs
namespace ProjectManagementAPI.DTO.Responses
{
    public class PersonalDashboardResponse
    {
        public DateTime Date { get; set; }
        public List<DailyTaskDetail> WorkedYesterday { get; set; } = new();
        public List<DailyTaskDetail> PlanForToday { get; set; } = new();
        public List<BlockerResponse> ActiveBlockers { get; set; } = new();

        public int TotalTasksAssigned { get; set; }
        public int TasksInProgress { get; set; }
        public int TasksCompletedToday { get; set; }
        public int OverdueTasks { get; set; }

        public List<NotificationResponse> Notifications { get; set; } = new();
    }

    public class DailyTaskDetail
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public Guid ProjectId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal? StoryPoints { get; set; }
        public decimal? EstimatedHours { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.Today && Status != "Done";
        public string? Notes { get; set; }
        public int SubTasksTotal { get; set; }
        public int SubTasksCompleted { get; set; }
    }

    public class DailyScrumResponse
    {
        public DateTime Date { get; set; }
        public List<TeamMemberDailyStatus> TeamMembers { get; set; } = new();
        public List<BlockerResponse> TeamBlockers { get; set; } = new();
        public SprintProgressResponse SprintProgress { get; set; } = new();
    }

    public class TeamMemberDailyStatus
    {
        public UserBriefResponse User { get; set; } = null!;
        public List<DailyTaskDetail> WorkedYesterday { get; set; } = new();
        public List<DailyTaskDetail> PlanForToday { get; set; } = new();
        public List<BlockerResponse> Blockers { get; set; } = new();
        public bool IsAvailable { get; set; } = true;
        public string? StatusNote { get; set; }
    }

    public class SprintProgressResponse
    {
        public Guid SprintId { get; set; }
        public string SprintName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TodoTasks { get; set; }
        public decimal CompletionPercentage { get; set; }
        public List<BurndownPoint> BurndownData { get; set; } = new();
    }
}