// Interfaces/IProjectService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface IProjectService
    {
        // Управление проектами
        Task<ApiResponse<ProjectResponse>> CreateProjectAsync(CreateProjectRequest request, Guid ownerId);
        Task<ApiResponse<ProjectResponse>> GetProjectByIdAsync(Guid projectId);
        Task<ApiResponse<PagedResult<ProjectResponse>>> GetUserProjectsAsync(Guid userId, PagedRequest request);
        Task<ApiResponse<ProjectResponse>> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request);
        Task<ApiResponse> DeleteProjectAsync(Guid projectId);
        Task<ApiResponse> ArchiveProjectAsync(Guid projectId);
        Task<ApiResponse> RestoreProjectAsync(Guid projectId);

        // Управление участниками
        Task<ApiResponse<List<ProjectMemberResponse>>> GetProjectMembersAsync(Guid projectId);
        Task<ApiResponse<ProjectMemberResponse>> AddMemberAsync(Guid projectId, AddProjectMemberRequest request, Guid currentUserId);
        Task<ApiResponse> UpdateMemberRoleAsync(Guid projectId, UpdateMemberRoleRequest request);
        Task<ApiResponse> RemoveMemberAsync(Guid projectId, RemoveMemberRequest request);

        // Статистика
        Task<ApiResponse<ProjectStatisticsResponse>> GetProjectStatisticsAsync(Guid projectId);

        // Проверка прав
        Task<bool> HasPermissionAsync(Guid projectId, Guid userId, string requiredRole);
    }
}