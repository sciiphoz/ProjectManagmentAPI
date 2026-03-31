// Controllers/DashboardController.cs
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
using ProjectManagementAPI.Extensions;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Получение персонального дашборда "Мой день"
        /// </summary>
        [HttpGet("my-day")]
        public async Task<ActionResult<ApiResponse<PersonalDashboardResponse>>> GetPersonalDashboard([FromQuery] DashboardRequest request)
        {
            var userId = User.GetUserId();
            var response = await _dashboardService.GetPersonalDashboardAsync(userId, request);
            return Ok(response);
        }

        /// <summary>
        /// Получение группового вида для Daily Scrum
        /// </summary>
        [HttpGet("daily-scrum")]
        public async Task<ActionResult<ApiResponse<DailyScrumResponse>>> GetDailyScrumView([FromQuery] Guid projectId, [FromQuery] Guid? sprintId = null)
        {
            var response = await _dashboardService.GetDailyScrumViewAsync(projectId, sprintId);
            return Ok(response);
        }

        /// <summary>
        /// Обновление ежедневных задач
        /// </summary>
        [HttpPost("my-day")]
        public async Task<ActionResult<ApiResponse>> UpdateDailyTasks(UpdateDailyTasksRequest request)
        {
            var userId = User.GetUserId();
            var response = await _dashboardService.UpdateDailyTasksAsync(userId, request);
            return Ok(response);
        }
    }
}