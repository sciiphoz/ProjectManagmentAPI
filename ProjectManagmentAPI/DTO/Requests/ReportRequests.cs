// DTO/Requests/ReportRequests.cs
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementAPI.DTO.Requests
{
    public class GenerateReportRequest
    {
        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public ReportType Type { get; set; } = ReportType.SprintReport;

        public ReportFormat Format { get; set; } = ReportFormat.PDF;

        public bool IncludeSubTasks { get; set; } = true;
        public bool IncludeComments { get; set; } = false;
        public bool IncludeActivityLog { get; set; } = true;

        // Для отчета по спринту
        public Guid? SprintId { get; set; }
    }

    public enum ReportType
    {
        SprintReport,
        ProjectProgress,
        TeamPerformance,
        VelocityReport,
        BurndownReport,
        TaskCompletionReport
    }

    public enum ReportFormat
    {
        PDF,
        Excel,
        CSV
    }
}