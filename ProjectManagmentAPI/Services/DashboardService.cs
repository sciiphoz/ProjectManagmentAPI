using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Models;
using System;
using System.Text.Json;

namespace ProjectManagementAPI.Services
{
    public class DashboardService : BaseService, IDashboardService
    {
        private readonly ContextDb _context;
        private readonly INotificationService _notificationService;

        public DashboardService(ContextDb context, ILogger<DashboardService> logger, INotificationService notificationService) : base(logger)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<PersonalDashboardResponse>> GetPersonalDashboardAsync(Guid userId, DashboardRequest? request = null)
        {
            try
            {
                var date = request?.Date ?? DateTime.UtcNow.Date;

                var assignedTasks = await _context.BacklogItems
                    .Include(bi => bi.Project)
                    .Include(bi => bi.SubTasks)
                    .Include(bi => bi.Sprint)
                    .Where(bi => bi.AssigneeId == userId && bi.SprintId != null)
                    .ToListAsync();

                var dailyTask = await _context.DailyUserTasks
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date);

                var workedYesterday = DeserializeOrGetDefault(assignedTasks, dailyTask?.WorkedYesterday, date);
                var planForToday = DeserializeOrGetDefault(assignedTasks, dailyTask?.PlanForToday, date);
                var blockers = DeserializeBlockersOrGetDefault(dailyTask?.Blockers);

                var overdueTasks = assignedTasks.Count(t => t.Status != BacklogItemStatus.Done && t.Sprint != null && t.Sprint.EndDate < DateTime.UtcNow.Date);

                return ApiResponse<PersonalDashboardResponse>.Ok(new PersonalDashboardResponse
                {
                    Date = date,
                    WorkedYesterday = workedYesterday,
                    PlanForToday = planForToday,
                    ActiveBlockers = blockers,
                    TotalTasksAssigned = assignedTasks.Count,
                    TasksInProgress = assignedTasks.Count(t => t.Status == BacklogItemStatus.InProgress),
                    TasksCompletedToday = assignedTasks.Count(t => t.CompletedAt.HasValue && t.CompletedAt.Value.Date == date),
                    OverdueTasks = overdueTasks,
                    Notifications = new()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения дашборда пользователя {UserId}", userId);
                return ApiResponse<PersonalDashboardResponse>.Fail("Произошла ошибка при загрузке дашборда");
            }
        }

        private List<DailyTaskDetail> DeserializeOrGetDefault(List<BacklogItem> tasks, string? json, DateTime date)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var deserialized = JsonSerializer.Deserialize<List<DailyTaskDetail>>(json);
                    if (deserialized != null && deserialized.Any()) return deserialized;
                }
                catch { }
            }
            return tasks.Where(t => t.Status == BacklogItemStatus.InProgress && t.UpdatedAt.HasValue && t.UpdatedAt.Value.Date == date.AddDays(-1))
                .Select(MapToDailyTaskDetail).ToList();
        }

        private List<BlockerResponse> DeserializeBlockersOrGetDefault(string? json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try { var d = JsonSerializer.Deserialize<List<BlockerResponse>>(json); if (d != null) return d; } catch { }
            }
            return new();
        }

        private static DailyTaskDetail MapToDailyTaskDetail(BacklogItem t) => new()
        {
            Id = t.Id,
            Title = t.Title,
            ProjectName = t.Project.Name,
            ProjectId = t.ProjectId,
            Status = t.Status.ToString(),
            Type = t.Type.ToString(),
            StoryPoints = t.StoryPoints,
            EstimatedHours = t.EstimatedHours,
            DueDate = t.Sprint?.EndDate,
            SubTasksTotal = t.SubTasks.Count,
            SubTasksCompleted = t.SubTasks.Count(st => st.Status == SubTaskStatus.Done)
        };

        public async Task<ApiResponse<DailyScrumResponse>> GetDailyScrumViewAsync(Guid projectId, Guid? sprintId = null)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    return ApiResponse<DailyScrumResponse>.Fail("Проект не найден");
                }

                var members = await _context.ProjectMembers
                    .Where(pm => pm.ProjectId == projectId)
                    .Include(pm => pm.User)
                    .ToListAsync();

                var sprint = sprintId.HasValue
                    ? await _context.Sprints.FindAsync(sprintId.Value)
                    : await _context.Sprints
                        .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.IsActive);

                var teamMembers = new List<TeamMemberDailyStatus>();

                foreach (var member in members)
                {
                    var dailyTask = await _context.DailyUserTasks
                        .FirstOrDefaultAsync(d => d.UserId == member.UserId && d.Date == DateTime.UtcNow.Date);

                    var workedYesterday = new List<DailyTaskDetail>();
                    var planForToday = new List<DailyTaskDetail>();
                    var blockers = new List<BlockerResponse>();

                    if (dailyTask != null)
                    {
                        if (!string.IsNullOrEmpty(dailyTask.WorkedYesterday))
                        {
                            try
                            {
                                workedYesterday = JsonSerializer.Deserialize<List<DailyTaskDetail>>(dailyTask.WorkedYesterday) ?? new();
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Ошибка десериализации WorkedYesterday для пользователя {UserId}", member.UserId);
                            }
                        }

                        if (!string.IsNullOrEmpty(dailyTask.PlanForToday))
                        {
                            try
                            {
                                planForToday = JsonSerializer.Deserialize<List<DailyTaskDetail>>(dailyTask.PlanForToday) ?? new();
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Ошибка десериализации PlanForToday для пользователя {UserId}", member.UserId);
                            }
                        }

                        if (!string.IsNullOrEmpty(dailyTask.Blockers))
                        {
                            try
                            {
                                blockers = JsonSerializer.Deserialize<List<BlockerResponse>>(dailyTask.Blockers) ?? new();
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Ошибка десериализации Blockers для пользователя {UserId}", member.UserId);
                            }
                        }
                    }

                    teamMembers.Add(new TeamMemberDailyStatus
                    {
                        User = new UserBriefResponse
                        {
                            Id = member.User.Id,
                            FullName = member.User.FullName,
                            Username = member.User.Username,
                            Email = member.User.Email,
                            Role = member.RoleInProject.ToString()
                        },
                        WorkedYesterday = workedYesterday,
                        PlanForToday = planForToday,
                        Blockers = blockers,
                        IsAvailable = true,
                        StatusNote = null
                    });
                }

                SprintProgressResponse? sprintProgress = null;
                if (sprint != null)
                {
                    var sprintTasks = await _context.BacklogItems
                        .Where(bi => bi.SprintId == sprint.Id)
                        .ToListAsync();

                    sprintProgress = new SprintProgressResponse
                    {
                        SprintId = sprint.Id,
                        SprintName = sprint.Name,
                        StartDate = sprint.StartDate,
                        EndDate = sprint.EndDate,
                        DaysRemaining = (sprint.EndDate - DateTime.UtcNow.Date).Days,
                        TotalTasks = sprintTasks.Count,
                        CompletedTasks = sprintTasks.Count(t => t.Status == BacklogItemStatus.Done),
                        InProgressTasks = sprintTasks.Count(t => t.Status == BacklogItemStatus.InProgress),
                        TodoTasks = sprintTasks.Count(t => t.Status == BacklogItemStatus.ToDo),
                        CompletionPercentage = sprintTasks.Any()
                            ? sprintTasks.Count(t => t.Status == BacklogItemStatus.Done) / sprintTasks.Count * 100
                            : 0
                    };
                }

                var teamBlockers = await _context.Blockers
                    .Where(b => b.BacklogItem!.ProjectId == projectId && b.Status == BlockerStatus.Active)
                    .Select(b => new BlockerResponse
                    {
                        Id = b.Id,
                        Description = b.Description,
                        Severity = b.Severity.ToString(),
                        Status = b.Status.ToString(),
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync();

                var response = new DailyScrumResponse
                {
                    Date = DateTime.UtcNow.Date,
                    TeamMembers = teamMembers,
                    TeamBlockers = teamBlockers,
                    SprintProgress = sprintProgress ?? new SprintProgressResponse()
                };

                return ApiResponse<DailyScrumResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения Daily Scrum для проекта {ProjectId}", projectId);
                return ApiResponse<DailyScrumResponse>.Fail("Произошла ошибка при загрузке данных");
            }
        }

        public async Task<ApiResponse> UpdateDailyTasksAsync(Guid userId, UpdateDailyTasksRequest request)
        {
            try
            {
                var dailyTask = await _context.DailyUserTasks
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == request.Date);

                var workedYesterdayJson = JsonSerializer.Serialize(request.WorkedYesterday);
                var planForTodayJson = JsonSerializer.Serialize(request.PlanForToday);
                var blockersJson = JsonSerializer.Serialize(request.Blockers);

                if (dailyTask == null)
                {
                    dailyTask = new DailyUserTask
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Date = request.Date,
                        WorkedYesterday = workedYesterdayJson,
                        PlanForToday = planForTodayJson,
                        Blockers = blockersJson,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.DailyUserTasks.Add(dailyTask);
                }
                else
                {
                    dailyTask.WorkedYesterday = workedYesterdayJson;
                    dailyTask.PlanForToday = planForTodayJson;
                    dailyTask.Blockers = blockersJson;
                    dailyTask.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return ApiResponse.Ok("Ежедневные задачи обновлены");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления ежедневных задач пользователя {UserId}", userId);
                return ApiResponse.Fail("Произошла ошибка при сохранении данных");
            }
        }
    }
}