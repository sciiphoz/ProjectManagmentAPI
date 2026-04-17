// Interfaces/IActivityLogService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface IActivityLogService
    {
        Task<ApiResponse<PagedResult<ActivityLogResponse>>> GetProjectLogsAsync(GetActivityLogsRequest request);
    }
}