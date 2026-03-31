// DTO/Requests/DashboardRequests.cs
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementAPI.DTO.Requests
{
    public class UpdateDailyTasksRequest
    {
        [Required]
        public DateTime Date { get; set; }

        public List<DailyTaskItem>? WorkedYesterday { get; set; }
        public List<DailyTaskItem>? PlanForToday { get; set; }
        public List<DailyBlockerItem>? Blockers { get; set; }
    }

    public class DailyTaskItem
    {
        public Guid TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class DailyBlockerItem
    {
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public Guid? TaskId { get; set; }
    }

    public class DashboardRequest
    {
        public DateTime? Date { get; set; }
        public Guid? ProjectId { get; set; }
        public bool IncludeAllProjects { get; set; } = false;
    }
}