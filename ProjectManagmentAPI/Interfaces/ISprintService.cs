// Interfaces/ISprintService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface ISprintService
    {
        // Управление спринтами
        Task<ApiResponse<SprintResponse>> CreateSprintAsync(CreateSprintRequest request);
        Task<ApiResponse<SprintResponse>> GetSprintByIdAsync(Guid sprintId);
        Task<ApiResponse<List<SprintBriefResponse>>> GetProjectSprintsAsync(Guid projectId);
        Task<ApiResponse<SprintResponse>> UpdateSprintAsync(Guid sprintId, UpdateSprintRequest request);
        Task<ApiResponse> DeleteSprintAsync(Guid sprintId);

        // Управление состоянием спринта
        Task<ApiResponse<SprintResponse>> StartSprintAsync(StartSprintRequest request);
        Task<ApiResponse<SprintResponse>> CompleteSprintAsync(CompleteSprintRequest request);
        Task<ApiResponse> CancelSprintAsync(Guid sprintId);

        // Доска спринта
        Task<ApiResponse<SprintBoardResponse>> GetSprintBoardAsync(Guid sprintId);
        Task<ApiResponse> UpdateTaskStatusAsync(Guid taskId, string newStatus);

        // Бэклог спринта
        Task<ApiResponse> MoveToSprintAsync(MoveToSprintRequest request);
        Task<ApiResponse> MoveToBacklogAsync(Guid backlogItemId);

        // Метрики
        Task<ApiResponse<SprintMetrics>> GetSprintMetricsAsync(Guid sprintId);
        Task<ApiResponse<List<BurndownPoint>>> GetBurndownChartAsync(Guid sprintId);

        // Заметки
        Task<ApiResponse> SaveReviewNotesAsync(Guid sprintId, string notes);
        Task<ApiResponse> SaveRetrospectiveNotesAsync(Guid sprintId, string notes);

        // История
        Task<ApiResponse<List<SprintVelocityHistory>>> GetSprintHistoryAsync(Guid projectId, int count = 5);
    }
}