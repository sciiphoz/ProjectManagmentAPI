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
    }
}