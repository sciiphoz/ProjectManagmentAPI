// Interfaces/ISubTaskService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface ISubTaskService
    {
        // Управление подзадачами
        Task<ApiResponse<SubTaskResponse>> CreateSubTaskAsync(CreateSubTaskRequest request);
        Task<ApiResponse<SubTaskResponse>> GetSubTaskByIdAsync(Guid subTaskId);
        Task<ApiResponse<List<SubTaskResponse>>> GetBacklogItemSubTasksAsync(Guid backlogItemId);
        Task<ApiResponse<SubTaskResponse>> UpdateSubTaskAsync(Guid subTaskId, UpdateSubTaskRequest request);
        Task<ApiResponse> DeleteSubTaskAsync(Guid subTaskId);

        // Управление статусом
        Task<ApiResponse<SubTaskResponse>> StartSubTaskAsync(StartSubTaskRequest request);
        Task<ApiResponse<SubTaskResponse>> CompleteSubTaskAsync(CompleteSubTaskRequest request);
        Task<ApiResponse<SubTaskResponse>> ChangeStatusAsync(Guid subTaskId, ChangeSubTaskStatusRequest request);

        // Управление порядком (drag-and-drop)
        Task<ApiResponse> ReorderSubTasksAsync(ReorderSubTasksRequest request);

        // Статистика
        Task<ApiResponse<SubTaskStatisticsResponse>> GetSubTaskStatisticsAsync(Guid backlogItemId);

        // Блокеры
        Task<ApiResponse<BlockerResponse>> AddBlockerToSubTaskAsync(Guid subTaskId, string description, string severity);
    }
}