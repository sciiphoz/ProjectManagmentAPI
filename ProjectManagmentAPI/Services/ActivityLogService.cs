// Services/ActivityLogService.cs
using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Models;
using System;

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
                    .Where(al => al.ProjectId == request.ProjectId)
                    .OrderByDescending(al => al.CreatedAt)
                    .AsQueryable();

                if (request.UserId.HasValue)
                {
                    query = query.Where(al => al.UserId == request.UserId.Value);
                }

                if (!string.IsNullOrEmpty(request.ActionType))
                {
                    query = query.Where(al => al.ActionType.ToString() == request.ActionType);
                }

                if (request.DateFrom.HasValue)
                {
                    query = query.Where(al => al.CreatedAt >= request.DateFrom.Value);
                }

                if (request.DateTo.HasValue)
                {
                    query = query.Where(al => al.CreatedAt <= request.DateTo.Value);
                }

                var totalCount = await query.CountAsync();

                var items = await query
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

                var result = new PagedResult<ActivityLogResponse>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                return ApiResponse<PagedResult<ActivityLogResponse>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения логов активности");
                return ApiResponse<PagedResult<ActivityLogResponse>>.Fail("Произошла ошибка при получении логов");
            }
        }
    }
}