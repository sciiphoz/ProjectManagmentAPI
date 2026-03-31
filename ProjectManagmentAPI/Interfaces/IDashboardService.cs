// Interfaces/IDashboardService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface IDashboardService
    {
        /// <summary>
        /// Получение персонального дашборда "Мой день"
        /// </summary>
        Task<ApiResponse<PersonalDashboardResponse>> GetPersonalDashboardAsync(Guid userId, DashboardRequest? request = null);

        /// <summary>
        /// Получение группового вида для Daily Scrum
        /// </summary>
        Task<ApiResponse<DailyScrumResponse>> GetDailyScrumViewAsync(Guid projectId, Guid? sprintId = null);

        /// <summary>
        /// Обновление ежедневных задач
        /// </summary>
        Task<ApiResponse> UpdateDailyTasksAsync(Guid userId, UpdateDailyTasksRequest request);
    }
}