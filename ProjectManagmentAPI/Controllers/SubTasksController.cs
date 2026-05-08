// Controllers/SubTasksController.cs
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
    public class SubTasksController : ControllerBase
    {
        private readonly ISubTaskService _subTaskService;
        private readonly ContextDb _context;

        public SubTasksController(ISubTaskService subTaskService, ContextDb context)
        {
            _subTaskService = subTaskService;
            _context = context;
        }

        private async Task<bool> IsViewerBySubTaskId(Guid subTaskId)
        {
            var subTask = await _context.SubTasks
                .Include(st => st.BacklogItem)
                .FirstOrDefaultAsync(st => st.Id == subTaskId);
            if (subTask?.BacklogItem == null) return true;

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var uid)) return true;

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == subTask.BacklogItem.ProjectId && pm.UserId == uid);
            return member?.RoleInProject == ProjectRole.Viewer;
        }

        private async Task<bool> IsViewerByBacklogItemId(Guid backlogItemId)
        {
            var item = await _context.BacklogItems.FindAsync(backlogItemId);
            if (item == null) return true;

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var uid)) return true;

            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == item.ProjectId && pm.UserId == uid);
            return member?.RoleInProject == ProjectRole.Viewer;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> CreateSubTask(CreateSubTaskRequest request)
        {
            var response = await _subTaskService.CreateSubTaskAsync(request);
            if (response.Success)
                return Ok(response);
            return BadRequest(response);
        }

        [HttpGet("{subTaskId}")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> GetSubTaskById(Guid subTaskId)
        {
            var response = await _subTaskService.GetSubTaskByIdAsync(subTaskId);
            return Ok(response);
        }

        [HttpGet("backlog-item/{backlogItemId}")]
        public async Task<ActionResult<ApiResponse<List<SubTaskResponse>>>> GetBacklogItemSubTasks(Guid backlogItemId)
        {
            var response = await _subTaskService.GetBacklogItemSubTasksAsync(backlogItemId);
            return Ok(response);
        }

        [HttpPut("{subTaskId}")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> UpdateSubTask(Guid subTaskId, UpdateSubTaskRequest request)
        {
            if (await IsViewerBySubTaskId(subTaskId)) return Forbid();
            var response = await _subTaskService.UpdateSubTaskAsync(subTaskId, request);
            return Ok(response);
        }

        [HttpDelete("{subTaskId}")]
        public async Task<ActionResult<ApiResponse>> DeleteSubTask(Guid subTaskId)
        {
            if (await IsViewerBySubTaskId(subTaskId)) return Forbid();
            var response = await _subTaskService.DeleteSubTaskAsync(subTaskId);
            return Ok(response);
        }

        [HttpPost("{subTaskId}/start")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> StartSubTask(Guid subTaskId)
        {
            if (await IsViewerBySubTaskId(subTaskId)) return Forbid();
            var request = new StartSubTaskRequest { SubTaskId = subTaskId };
            var response = await _subTaskService.StartSubTaskAsync(request);
            return Ok(response);
        }

        [HttpPost("{subTaskId}/complete")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> CompleteSubTask(Guid subTaskId, CompleteSubTaskRequest request)
        {
            if (await IsViewerBySubTaskId(subTaskId)) return Forbid();
            request.SubTaskId = subTaskId;
            var response = await _subTaskService.CompleteSubTaskAsync(request);
            return Ok(response);
        }

        [HttpPatch("{subTaskId}/status")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> ChangeStatus(Guid subTaskId, ChangeSubTaskStatusRequest request)
        {
            if (await IsViewerBySubTaskId(subTaskId)) return Forbid();
            var response = await _subTaskService.ChangeStatusAsync(subTaskId, request);
            return Ok(response);
        }

        [HttpPost("reorder")]
        public async Task<ActionResult<ApiResponse>> ReorderSubTasks(ReorderSubTasksRequest request)
        {
            var response = await _subTaskService.ReorderSubTasksAsync(request);
            return Ok(response);
        }

        [HttpGet("backlog-item/{backlogItemId}/statistics")]
        public async Task<ActionResult<ApiResponse<SubTaskStatisticsResponse>>> GetSubTaskStatistics(Guid backlogItemId)
        {
            var response = await _subTaskService.GetSubTaskStatisticsAsync(backlogItemId);
            return Ok(response);
        }

        [HttpPost("{subTaskId}/blockers")]
        public async Task<ActionResult<ApiResponse<BlockerResponse>>> AddBlocker(Guid subTaskId, AddBlockerRequest request)
        {
            if (await IsViewerBySubTaskId(subTaskId)) return Forbid();
            var response = await _subTaskService.AddBlockerToSubTaskAsync(subTaskId, request.Description, request.Severity);
            return Ok(response);
        }
    }
}