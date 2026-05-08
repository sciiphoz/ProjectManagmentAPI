// Controllers/RetrospectiveController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Extensions;
using ProjectManagementAPI.Interfaces;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RetrospectiveController : ControllerBase
    {
        private readonly IRetrospectiveService _retrospectiveService;
        private readonly ContextDb _context;

        public RetrospectiveController(IRetrospectiveService retrospectiveService, ContextDb context)
        {
            _retrospectiveService = retrospectiveService;
            _context = context;
        }

        private async Task<bool> IsViewerBySprintId(Guid sprintId)
        {
            var sprint = await _context.Sprints.FindAsync(sprintId);
            if (sprint == null) return true;

            var userId = User.GetUserId();
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == sprint.ProjectId && pm.UserId == userId);
            return member?.RoleInProject == ProjectRole.Viewer;
        }

        [HttpGet("sprint/{sprintId}")]
        public async Task<ActionResult<ApiResponse<RetrospectiveBoardResponse>>> GetRetrospectiveBoard(Guid sprintId)
        {
            var userId = User.GetUserId();
            var response = await _retrospectiveService.GetRetrospectiveBoardAsync(sprintId, userId);
            return Ok(response);
        }

        [HttpPost("sprint/{sprintId}/items")]
        public async Task<ActionResult<ApiResponse<RetrospectiveItemResponse>>> AddRetrospectiveItem(
            Guid sprintId, [FromBody] AddRetrospectiveItemRequest request)
        {
            if (await IsViewerBySprintId(sprintId)) return Forbid();
            var userId = User.GetUserId();
            var response = await _retrospectiveService.AddRetrospectiveItemAsync(
                sprintId, request.Category, request.Content, userId);
            return Ok(response);
        }

        [HttpPost("items/{itemId}/vote")]
        public async Task<ActionResult<ApiResponse>> VoteRetrospectiveItem(Guid itemId)
        {
            var userId = User.GetUserId();
            var response = await _retrospectiveService.VoteRetrospectiveItemAsync(itemId, userId);
            return Ok(response);
        }

        [HttpDelete("items/{itemId}/vote")]
        public async Task<ActionResult<ApiResponse>> RemoveVote(Guid itemId)
        {
            var userId = User.GetUserId();
            var response = await _retrospectiveService.RemoveVoteAsync(itemId, userId);
            return Ok(response);
        }

        [HttpDelete("items/{itemId}")]
        public async Task<ActionResult<ApiResponse>> DeleteRetrospectiveItem(Guid itemId)
        {
            var userId = User.GetUserId();
            var response = await _retrospectiveService.DeleteRetrospectiveItemAsync(itemId, userId);
            return Ok(response);
        }
    }
}