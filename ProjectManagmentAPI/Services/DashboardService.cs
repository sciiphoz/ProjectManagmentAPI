using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.Models;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.DataBaseContext;
using System;
using System.Text.Json;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.Enums;

namespace ProjectManagementAPI.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ContextDb _context;

        public DashboardService(ContextDb context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PersonalDashboardResponse>> GetPersonalDashboardAsync(Guid userId, DashboardRequest? request = null)
        {
            var date = request?.Date ?? DateTime.UtcNow.Date;

            // Получаем задачи, назначенные пользователю
            var assignedTasks = await _context.BacklogItems
                .Include(bi => bi.Project)
                .Where(bi => bi.AssigneeId == userId && bi.SprintId != null)
                .ToListAsync();

            // Задачи, над которыми работал вчера (были в статусе InProgress)
            var workedYesterday = assignedTasks
                .Where(t => t.Status == BacklogItemStatus.InProgress &&
                            t.UpdatedAt.HasValue &&
                            t.UpdatedAt.Value.Date == date.AddDays(-1))
                .Select(t => new DailyTaskDetail
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
                })
                .ToList();

            // Задачи на сегодня (To Do и In Progress)
            var planForToday = assignedTasks
                .Where(t => t.Status == BacklogItemStatus.ToDo || t.Status == BacklogItemStatus.InProgress)
                .Select(t => new DailyTaskDetail
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
                })
                .ToList();

            // Активные блокеры
            var blockers = await _context.Blockers
                .Where(b => b.BacklogItem!.AssigneeId == userId && b.Status == BlockerStatus.Active)
                .Select(b => new BlockerResponse
                {
                    Id = b.Id,
                    Description = b.Description,
                    Severity = b.Severity.ToString(),
                    Status = b.Status.ToString(),
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            // Получаем сохраненные ежедневные задачи из DailyUserTasks
            var dailyTask = await _context.DailyUserTasks
                .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date);

            if (dailyTask != null)
            {
                if (!string.IsNullOrEmpty(dailyTask.WorkedYesterday))
                {
                    var savedWorkedYesterday = JsonSerializer.Deserialize<List<DailyTaskDetail>>(dailyTask.WorkedYesterday);
                    if (savedWorkedYesterday != null && savedWorkedYesterday.Any())
                    {
                        workedYesterday = savedWorkedYesterday;
                    }
                }

                if (!string.IsNullOrEmpty(dailyTask.PlanForToday))
                {
                    var savedPlanForToday = JsonSerializer.Deserialize<List<DailyTaskDetail>>(dailyTask.PlanForToday);
                    if (savedPlanForToday != null && savedPlanForToday.Any())
                    {
                        planForToday = savedPlanForToday;
                    }
                }

                if (!string.IsNullOrEmpty(dailyTask.Blockers))
                {
                    var savedBlockers = JsonSerializer.Deserialize<List<BlockerResponse>>(dailyTask.Blockers);
                    if (savedBlockers != null && savedBlockers.Any())
                    {
                        blockers = savedBlockers;
                    }
                }
            }

            var overdueTasks = assignedTasks
                .Count(t => t.Status != BacklogItemStatus.Done &&
                            t.Sprint != null &&
                            t.Sprint.EndDate < DateTime.UtcNow.Date);

            var response = new PersonalDashboardResponse
            {
                Date = date,
                WorkedYesterday = workedYesterday,
                PlanForToday = planForToday,
                ActiveBlockers = blockers,
                TotalTasksAssigned = assignedTasks.Count,
                TasksInProgress = assignedTasks.Count(t => t.Status == BacklogItemStatus.InProgress),
                TasksCompletedToday = assignedTasks.Count(t => t.CompletedAt.HasValue && t.CompletedAt.Value.Date == date),
                OverdueTasks = overdueTasks,
                Notifications = new List<NotificationResponse>()
            };

            return ApiResponse<PersonalDashboardResponse>.Ok(response);
        }

        public async Task<ApiResponse<DailyScrumResponse>> GetDailyScrumViewAsync(Guid projectId, Guid? sprintId = null)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                return ApiResponse<DailyScrumResponse>.Fail("Проект не найден");
            }

            // Получаем всех участников проекта
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
                // Получаем ежедневные задачи участника
                var dailyTask = await _context.DailyUserTasks
                    .FirstOrDefaultAsync(d => d.UserId == member.UserId && d.Date == DateTime.UtcNow.Date);

                var workedYesterday = new List<DailyTaskDetail>();
                var planForToday = new List<DailyTaskDetail>();
                var blockers = new List<BlockerResponse>();

                if (dailyTask != null)
                {
                    if (!string.IsNullOrEmpty(dailyTask.WorkedYesterday))
                    {
                        workedYesterday = JsonSerializer.Deserialize<List<DailyTaskDetail>>(dailyTask.WorkedYesterday) ?? new();
                    }

                    if (!string.IsNullOrEmpty(dailyTask.PlanForToday))
                    {
                        planForToday = JsonSerializer.Deserialize<List<DailyTaskDetail>>(dailyTask.PlanForToday) ?? new();
                    }

                    if (!string.IsNullOrEmpty(dailyTask.Blockers))
                    {
                        blockers = JsonSerializer.Deserialize<List<BlockerResponse>>(dailyTask.Blockers) ?? new();
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

            // Прогресс спринта
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
                    CompletionPercentage = (decimal)(sprintTasks.Any()
                        ? (double)sprintTasks.Count(t => t.Status == BacklogItemStatus.Done) / sprintTasks.Count * 100
                        : 0)
                };
            }

            // Командные блокеры
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

        public async Task<ApiResponse> UpdateDailyTasksAsync(Guid userId, UpdateDailyTasksRequest request)
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
    }
}