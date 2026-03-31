// Controllers/ReportsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Генерация отчёта по спринту
        /// </summary>
        [HttpGet("sprint/{sprintId}")]
        public async Task<ActionResult<ApiResponse<SprintReportResponse>>> GenerateSprintReport(Guid sprintId)
        {
            var response = await _reportService.GenerateSprintReportAsync(sprintId);
            return Ok(response);
        }

        /// <summary>
        /// Генерация отчёта по производительности команды
        /// </summary>
        [HttpPost("team-performance")]
        public async Task<ActionResult<ApiResponse<TeamPerformanceReportResponse>>> GenerateTeamPerformanceReport(GenerateReportRequest request)
        {
            var response = await _reportService.GenerateTeamPerformanceReportAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Генерация Velocity отчёта
        /// </summary>
        [HttpGet("velocity/{projectId}")]
        public async Task<ActionResult<ApiResponse<VelocityReportResponse>>> GenerateVelocityReport(Guid projectId, [FromQuery] int lastSprintsCount = 5)
        {
            var response = await _reportService.GenerateVelocityReportAsync(projectId, lastSprintsCount);
            return Ok(response);
        }

        /// <summary>
        /// Экспорт отчёта в файл
        /// </summary>
        [HttpPost("export")]
        public async Task<IActionResult> ExportReport(GenerateReportRequest request)
        {
            var fileData = await _reportService.ExportReportAsync(request);

            string extension = request.Format switch
            {
                ReportFormat.PDF => "pdf",
                ReportFormat.Excel => "xlsx",
                ReportFormat.CSV => "csv",
                _ => "pdf"
            };

            string contentType = request.Format switch
            {
                ReportFormat.PDF => "application/pdf",
                ReportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ReportFormat.CSV => "text/csv",
                _ => "application/pdf"
            };

            return File(fileData, contentType, $"report_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");
        }
    }
}