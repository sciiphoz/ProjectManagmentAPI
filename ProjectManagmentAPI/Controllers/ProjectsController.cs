// Controllers/ProjectsController.cs
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
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;

        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        /// <summary>
        /// Создание нового проекта
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectResponse>>> CreateProject(CreateProjectRequest request)
        {
            var userId = User.GetUserId();
            var response = await _projectService.CreateProjectAsync(request, userId);
            return CreatedAtAction(nameof(GetProjectById), new { projectId = response.Data?.Id }, response);
        }

        /// <summary>
        /// Получение проекта по ID
        /// </summary>
        [HttpGet("{projectId}")]
        public async Task<ActionResult<ApiResponse<ProjectResponse>>> GetProjectById(Guid projectId)
        {
            var response = await _projectService.GetProjectByIdAsync(projectId);
            return Ok(response);
        }

        /// <summary>
        /// Получение проектов текущего пользователя
        /// </summary>
        [HttpGet("my")]
        public async Task<ActionResult<ApiResponse<PagedResult<ProjectResponse>>>> GetMyProjects([FromQuery] PagedRequest request)
        {
            var userId = User.GetUserId();
            var response = await _projectService.GetUserProjectsAsync(userId, request);
            return Ok(response);
        }

        /// <summary>
        /// Обновление проекта
        /// </summary>
        [HttpPut("{projectId}")]
        public async Task<ActionResult<ApiResponse<ProjectResponse>>> UpdateProject(Guid projectId, UpdateProjectRequest request)
        {
            var response = await _projectService.UpdateProjectAsync(projectId, request);
            return Ok(response);
        }

        /// <summary>
        /// Архивация проекта
        /// </summary>
        [HttpPost("{projectId}/archive")]
        public async Task<ActionResult<ApiResponse>> ArchiveProject(Guid projectId)
        {
            var response = await _projectService.ArchiveProjectAsync(projectId);
            return Ok(response);
        }

        /// <summary>
        /// Восстановление проекта из архива
        /// </summary>
        [HttpPost("{projectId}/restore")]
        public async Task<ActionResult<ApiResponse>> RestoreProject(Guid projectId)
        {
            var response = await _projectService.RestoreProjectAsync(projectId);
            return Ok(response);
        }

        /// <summary>
        /// Удаление проекта (только для владельца)
        /// </summary>
        [HttpDelete("{projectId}")]
        public async Task<ActionResult<ApiResponse>> DeleteProject(Guid projectId)
        {
            var response = await _projectService.DeleteProjectAsync(projectId);
            return Ok(response);
        }

        /// <summary>
        /// Получение участников проекта
        /// </summary>
        [HttpGet("{projectId}/members")]
        public async Task<ActionResult<ApiResponse<List<ProjectMemberResponse>>>> GetProjectMembers(Guid projectId)
        {
            var response = await _projectService.GetProjectMembersAsync(projectId);
            return Ok(response);
        }

        /// <summary>
        /// Добавление участника в проект
        /// </summary>
        [HttpPost("{projectId}/members")]
        public async Task<ActionResult<ApiResponse<ProjectMemberResponse>>> AddMember(Guid projectId, AddProjectMemberRequest request)
        {
            var response = await _projectService.AddMemberAsync(projectId, request);
            return Ok(response);
        }

        /// <summary>
        /// Обновление роли участника
        /// </summary>
        [HttpPut("{projectId}/members/{userId}/role")]
        public async Task<ActionResult<ApiResponse>> UpdateMemberRole(Guid projectId, Guid userId, UpdateMemberRoleRequest request)
        {
            request.UserId = userId;
            var response = await _projectService.UpdateMemberRoleAsync(projectId, request);
            return Ok(response);
        }

        /// <summary>
        /// Удаление участника из проекта
        /// </summary>
        [HttpDelete("{projectId}/members/{userId}")]
        public async Task<ActionResult<ApiResponse>> RemoveMember(Guid projectId, Guid userId)
        {
            var request = new RemoveMemberRequest { UserId = userId };
            var response = await _projectService.RemoveMemberAsync(projectId, request);
            return Ok(response);
        }

        /// <summary>
        /// Получение статистики проекта
        /// </summary>
        [HttpGet("{projectId}/statistics")]
        public async Task<ActionResult<ApiResponse<ProjectStatisticsResponse>>> GetProjectStatistics(Guid projectId)
        {
            var response = await _projectService.GetProjectStatisticsAsync(projectId);
            return Ok(response);
        }
    }
}