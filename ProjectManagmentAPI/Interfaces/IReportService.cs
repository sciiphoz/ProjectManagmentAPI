// Interfaces/IReportService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface IReportService
    {
        /// <summary>
        /// Генерация отчета по спринту
        /// </summary>
        Task<ApiResponse<SprintReportResponse>> GenerateSprintReportAsync(Guid sprintId);

        /// <summary>
        /// Генерация отчета по производительности команды
        /// </summary>
        Task<ApiResponse<TeamPerformanceReportResponse>> GenerateTeamPerformanceReportAsync(GenerateReportRequest request);

        /// <summary>
        /// Генерация Velocity отчета
        /// </summary>
        Task<ApiResponse<VelocityReportResponse>> GenerateVelocityReportAsync(Guid projectId, int lastSprintsCount = 5);
    }
}