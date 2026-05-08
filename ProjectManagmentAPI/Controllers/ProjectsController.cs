// Controllers/ProjectsController.cs
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
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        private readonly ContextDb _context;

        public ProjectsController(IProjectService projectService, ContextDb context)
        {
            _projectService = projectService;
            _context = context;
        }

        private async Task<bool> IsViewer(Guid projectId)
        {
            var userId = User.GetUserId();
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            return member?.RoleInProject == ProjectRole.Viewer;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectResponse>>> CreateProject(CreateProjectRequest request)
        {
            var userId = User.GetUserId();
            var response = await _projectService.CreateProjectAsync(request, userId);
            return CreatedAtAction(nameof(GetProjectById), new { projectId = response.Data?.Id }, response);
        }

        [HttpGet("{projectId}")]
        public async Task<ActionResult<ApiResponse<ProjectResponse>>> GetProjectById(Guid projectId)
        {
            var response = await _projectService.GetProjectByIdAsync(projectId);
            return Ok(response);
        }

        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<ProjectResponse>>>> GetMyProjects([FromQuery] PagedRequest request)
        {
            var userId = User.GetUserId();
            var response = await _projectService.GetUserProjectsAsync(userId, request);
            return Ok(response);
        }

        [HttpPut("{projectId}")]
        public async Task<ActionResult<ApiResponse<ProjectResponse>>> UpdateProject(Guid projectId, UpdateProjectRequest request)
        {
            if (await IsViewer(projectId)) return Forbid();
            var response = await _projectService.UpdateProjectAsync(projectId, request);
            return Ok(response);
        }

        [HttpPost("{projectId}/archive")]
        public async Task<ActionResult<ApiResponse>> ArchiveProject(Guid projectId)
        {
            if (await IsViewer(projectId)) return Forbid();
            var response = await _projectService.ArchiveProjectAsync(projectId);
            return Ok(response);
        }

        [HttpPost("{projectId}/restore")]
        public async Task<ActionResult<ApiResponse>> RestoreProject(Guid projectId)
        {
            if (await IsViewer(projectId)) return Forbid();
            var response = await _projectService.RestoreProjectAsync(projectId);
            return Ok(response);
        }

        [HttpDelete("{projectId}")]
        public async Task<ActionResult<ApiResponse>> DeleteProject(Guid projectId)
        {
            if (await IsViewer(projectId)) return Forbid();
            var response = await _projectService.DeleteProjectAsync(projectId);
            return Ok(response);
        }

        [HttpGet("{projectId}/members")]
        public async Task<ActionResult<ApiResponse<List<ProjectMemberResponse>>>> GetProjectMembers(Guid projectId)
        {
            var response = await _projectService.GetProjectMembersAsync(projectId);
            return Ok(response);
        }

        [HttpPost("{projectId}/members")]
        public async Task<ActionResult<ApiResponse<ProjectMemberResponse>>> AddMember(Guid projectId, AddProjectMemberRequest request)
        {
            if (await IsViewer(projectId)) return Forbid();
            var currentUserId = User.GetUserId();
            var response = await _projectService.AddMemberAsync(projectId, request, currentUserId);
            return Ok(response);
        }

        [HttpPut("{projectId}/members/{userId}/role")]
        public async Task<ActionResult<ApiResponse>> UpdateMemberRole(Guid projectId, Guid userId, UpdateMemberRoleRequest request)
        {
            if (await IsViewer(projectId)) return Forbid();
            request.UserId = userId;
            var response = await _projectService.UpdateMemberRoleAsync(projectId, request);
            return Ok(response);
        }

        [HttpDelete("{projectId}/members/{userId}")]
        public async Task<ActionResult<ApiResponse>> RemoveMember(Guid projectId, Guid userId)
        {
            if (await IsViewer(projectId)) return Forbid();
            var request = new RemoveMemberRequest { UserId = userId };
            var response = await _projectService.RemoveMemberAsync(projectId, request);
            return Ok(response);
        }

        [HttpGet("{projectId}/statistics")]
        public async Task<ActionResult<ApiResponse<ProjectStatisticsResponse>>> GetProjectStatistics(Guid projectId)
        {
            var response = await _projectService.GetProjectStatisticsAsync(projectId);
            return Ok(response);
        }
    }
}