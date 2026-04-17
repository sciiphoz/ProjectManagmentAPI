// Controllers/ActivityLogsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;

namespace ProjectManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivityLogsController : ControllerBase
    {
        private readonly IActivityLogService _activityLogService;
        private readonly ILogger<ActivityLogsController> _logger;

        public ActivityLogsController(IActivityLogService activityLogService, ILogger<ActivityLogsController> logger)
        {
            _activityLogService = activityLogService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<ActivityLogResponse>>>> GetProjectLogs(
            [FromQuery] Guid projectId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? userId = null,
            [FromQuery] string? actionType = null,
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null)
        {
            try
            {
                var request = new GetActivityLogsRequest
                {
                    ProjectId = projectId,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    UserId = userId,
                    ActionType = actionType,
                    DateFrom = dateFrom,
                    DateTo = dateTo
                };

                var response = await _activityLogService.GetProjectLogsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения логов активности для проекта {ProjectId}", projectId);
                return StatusCode(500, ApiResponse<PagedResult<ActivityLogResponse>>.Fail("Внутренняя ошибка сервера"));
            }
        }
    }
}