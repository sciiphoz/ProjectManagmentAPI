using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Interfaces;

namespace ProjectManagementAPI.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly ContextDb _context;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(ContextDb context, ILogger<ActivityLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<PagedResult<ActivityLogResponse>>> GetProjectLogsAsync(GetActivityLogsRequest request)
        {
            try
            {
                var query = _context.ActivityLogs
                    .Include(al => al.User)
                    .Where(al => al.ProjectId == request.ProjectId);

                if (request.UserId.HasValue)
                    query = query.Where(al => al.UserId == request.UserId.Value);

                if (!string.IsNullOrEmpty(request.ActionType) && Enum.TryParse<ActionType>(request.ActionType, out var actionType))
                    query = query.Where(al => al.ActionType == actionType);

                if (request.DateFrom.HasValue)
                    query = query.Where(al => al.CreatedAt >= request.DateFrom.Value);

                if (request.DateTo.HasValue)
                    query = query.Where(al => al.CreatedAt <= request.DateTo.Value);

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(al => al.CreatedAt)
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(al => new ActivityLogResponse
                    {
                        Id = al.Id,
                        ActionType = al.ActionType.ToString(),
                        Description = al.Description,
                        User = new UserBriefResponse
                        {
                            Id = al.User.Id,
                            FullName = al.User.FullName,
                            Username = al.User.Username,
                            Email = al.User.Email
                        },
                        OldValue = al.OldValue,
                        NewValue = al.NewValue,
                        CreatedAt = al.CreatedAt
                    })
                    .ToListAsync();

                return ApiResponse<PagedResult<ActivityLogResponse>>.Ok(new PagedResult<ActivityLogResponse>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения логов активности");
                return ApiResponse<PagedResult<ActivityLogResponse>>.Fail("Произошла ошибка при получении логов");
            }
        }
    }
}