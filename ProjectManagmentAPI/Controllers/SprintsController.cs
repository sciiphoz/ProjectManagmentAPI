using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Interfaces;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SprintsController : ControllerBase
    {
        private readonly ISprintService _sprintService;
        private readonly ContextDb _context;

        public SprintsController(ISprintService sprintService, ContextDb context)
        {
            _sprintService = sprintService;
            _context = context;
        }

        /// <summary>
        /// Проверяет, является ли текущий пользователь Viewer в проекте
        /// </summary>
        private async Task<bool> IsViewerByProjectId(Guid projectId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var uid))
                return true;

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == uid);
            return member?.RoleInProject == ProjectRole.Viewer;
        }

        /// <summary>
        /// Проверяет, является ли текущий пользователь Viewer в проекте спринта
        /// </summary>
        private async Task<bool> IsViewerBySprintId(Guid sprintId)
        {
            var sprint = await _context.Sprints.FindAsync(sprintId);
            if (sprint == null) return true;
            return await IsViewerByProjectId(sprint.ProjectId);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> CreateSprint(CreateSprintRequest request)
        {
            if (await IsViewerByProjectId(request.ProjectId))
                return Forbid();

            var response = await _sprintService.CreateSprintAsync(request);
            return CreatedAtAction(nameof(GetSprintById), new { sprintId = response.Data?.Id }, response);
        }

        [HttpGet("{sprintId}")]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> GetSprintById(Guid sprintId)
        {
            var response = await _sprintService.GetSprintByIdAsync(sprintId);
            return Ok(response);
        }

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<ApiResponse<List<SprintBriefResponse>>>> GetProjectSprints(Guid projectId)
        {
            var response = await _sprintService.GetProjectSprintsAsync(projectId);
            return Ok(response);
        }

        [HttpPut("{sprintId}")]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> UpdateSprint(Guid sprintId, UpdateSprintRequest request)
        {
            if (await IsViewerBySprintId(sprintId))
                return Forbid();

            var response = await _sprintService.UpdateSprintAsync(sprintId, request);
            return Ok(response);
        }

        [HttpPost("{sprintId}/start")]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> StartSprint(Guid sprintId, StartSprintRequest request)
        {
            if (await IsViewerBySprintId(sprintId))
                return Forbid();

            request.SprintId = sprintId;
            var response = await _sprintService.StartSprintAsync(request);
            return Ok(response);
        }

        [HttpPost("{sprintId}/complete")]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> CompleteSprint(Guid sprintId, CompleteSprintRequest request)
        {
            if (await IsViewerBySprintId(sprintId))
                return Forbid();

            request.SprintId = sprintId;
            var response = await _sprintService.CompleteSprintAsync(request);
            return Ok(response);
        }

        [HttpPost("{sprintId}/cancel")]
        public async Task<ActionResult<ApiResponse>> CancelSprint(Guid sprintId)
        {
            if (await IsViewerBySprintId(sprintId))
                return Forbid();

            var response = await _sprintService.CancelSprintAsync(sprintId);
            return Ok(response);
        }

        [HttpDelete("{sprintId}")]
        public async Task<ActionResult<ApiResponse>> DeleteSprint(Guid sprintId)
        {
            if (await IsViewerBySprintId(sprintId))
                return Forbid();

            var response = await _sprintService.DeleteSprintAsync(sprintId);
            return Ok(response);
        }

        [HttpGet("{sprintId}/board")]
        public async Task<ActionResult<ApiResponse<SprintBoardResponse>>> GetSprintBoard(Guid sprintId)
        {
            var response = await _sprintService.GetSprintBoardAsync(sprintId);
            return Ok(response);
        }

        [HttpPatch("tasks/{taskId}/status")]
        public async Task<ActionResult<ApiResponse>> UpdateTaskStatus(Guid taskId, [FromBody] string newStatus)
        {
            var task = await _context.BacklogItems.FindAsync(taskId);
            if (task != null && await IsViewerByProjectId(task.ProjectId))
                return Forbid();

            var response = await _sprintService.UpdateTaskStatusAsync(taskId, newStatus);
            return Ok(response);
        }

        [HttpPost("move-to-sprint")]
        public async Task<ActionResult<ApiResponse>> MoveToSprint(MoveToSprintRequest request)
        {
            var sprint = await _context.Sprints.FindAsync(request.SprintId);
            if (sprint != null && await IsViewerByProjectId(sprint.ProjectId))
                return Forbid();

            var response = await _sprintService.MoveToSprintAsync(request);
            return Ok(response);
        }

        [HttpPost("{backlogItemId}/move-to-backlog")]
        public async Task<ActionResult<ApiResponse>> MoveToBacklog(Guid backlogItemId)
        {
            var item = await _context.BacklogItems.FindAsync(backlogItemId);
            if (item != null && await IsViewerByProjectId(item.ProjectId))
                return Forbid();

            var response = await _sprintService.MoveToBacklogAsync(backlogItemId);
            return Ok(response);
        }

        [HttpGet("{sprintId}/metrics")]
        public async Task<ActionResult<ApiResponse<SprintMetrics>>> GetSprintMetrics(Guid sprintId)
        {
            var response = await _sprintService.GetSprintMetricsAsync(sprintId);
            return Ok(response);
        }

        [HttpGet("{sprintId}/burndown")]
        public async Task<ActionResult<ApiResponse<List<BurndownPoint>>>> GetBurndownChart(Guid sprintId)
        {
            var response = await _sprintService.GetBurndownChartAsync(sprintId);
            return Ok(response);
        }

        [HttpPost("{sprintId}/review-notes")]
        public async Task<ActionResult<ApiResponse>> SaveReviewNotes(Guid sprintId, [FromBody] string notes)
        {
            if (await IsViewerBySprintId(sprintId))
                return Forbid();

            var response = await _sprintService.SaveReviewNotesAsync(sprintId, notes);
            return Ok(response);
        }

        [HttpPost("{sprintId}/retrospective-notes")]
        public async Task<ActionResult<ApiResponse>> SaveRetrospectiveNotes(Guid sprintId, [FromBody] string notes)
        {
            if (await IsViewerBySprintId(sprintId))
                return Forbid();

            var response = await _sprintService.SaveRetrospectiveNotesAsync(sprintId, notes);
            return Ok(response);
        }

        [HttpGet("project/{projectId}/history")]
        public async Task<ActionResult<ApiResponse<List<SprintVelocityHistory>>>> GetSprintHistory(Guid projectId, [FromQuery] int count = 5)
        {
            var response = await _sprintService.GetSprintHistoryAsync(projectId, count);
            return Ok(response);
        }
    }
}