using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.Models;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.DTO.Requests;

namespace ProjectManagementAPI.Services
{
    public class SprintService : ISprintService
    {
        private readonly ContextDb _context;
        private readonly INotificationService _notificationService;

        public SprintService(ContextDb context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<SprintResponse>> CreateSprintAsync(CreateSprintRequest request)
        {
            var project = await _context.Projects.FindAsync(request.ProjectId);
            if (project == null)
            {
                return ApiResponse<SprintResponse>.Fail("Проект не найден");
            }

            var sprint = new Sprint
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Name = request.Name,
                Goal = request.Goal,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                IsActive = false,
                Status = SprintStatus.Planned,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sprints.Add(sprint);
            await _context.SaveChangesAsync();

            var response = await MapToSprintResponse(sprint);
            return ApiResponse<SprintResponse>.Ok(response, "Спринт создан");
        }

        public async Task<ApiResponse<SprintResponse>> GetSprintByIdAsync(Guid sprintId)
        {
            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return ApiResponse<SprintResponse>.Fail("Спринт не найден");
            }

            var response = await MapToSprintResponse(sprint);
            return ApiResponse<SprintResponse>.Ok(response);
        }

        public async Task<ApiResponse<List<SprintBriefResponse>>> GetProjectSprintsAsync(Guid projectId)
        {
            var sprints = await _context.Sprints
                .Where(s => s.ProjectId == projectId)
                .OrderByDescending(s => s.StartDate)
                .Select(s => new SprintBriefResponse
                {
                    Id = s.Id,
                    Name = s.Name,
                    Status = s.Status.ToString(),
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return ApiResponse<List<SprintBriefResponse>>.Ok(sprints);
        }

        public async Task<ApiResponse<SprintResponse>> UpdateSprintAsync(Guid sprintId, UpdateSprintRequest request)
        {
            var sprint = await _context.Sprints.FindAsync(sprintId);
            if (sprint == null)
            {
                return ApiResponse<SprintResponse>.Fail("Спринт не найден");
            }

            if (request.Name != null)
                sprint.Name = request.Name;

            if (request.Goal != null)
                sprint.Goal = request.Goal;

            if (request.StartDate.HasValue)
                sprint.StartDate = request.StartDate.Value;

            if (request.EndDate.HasValue)
                sprint.EndDate = request.EndDate.Value;

            if (request.Status != null && Enum.TryParse<SprintStatus>(request.Status, true, out var status))
                sprint.Status = status;

            await _context.SaveChangesAsync();

            var response = await MapToSprintResponse(sprint);
            return ApiResponse<SprintResponse>.Ok(response, "Спринт обновлен");
        }

        public async Task<ApiResponse> DeleteSprintAsync(Guid sprintId)
        {
            var sprint = await _context.Sprints
                .Include(s => s.BacklogItems)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return ApiResponse.Fail("Спринт не найден");
            }

            foreach (var item in sprint.BacklogItems)
            {
                item.SprintId = null;
                item.Status = BacklogItemStatus.Backlog;
            }

            _context.Sprints.Remove(sprint);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Спринт удален");
        }

        public async Task<ApiResponse<SprintResponse>> StartSprintAsync(StartSprintRequest request)
        {
            var sprint = await _context.Sprints.FindAsync(request.SprintId);
            if (sprint == null)
            {
                return ApiResponse<SprintResponse>.Fail("Спринт не найден");
            }

            if (sprint.IsActive)
            {
                return ApiResponse<SprintResponse>.Fail("Спринт уже активен");
            }

            for (int i = 0; i < request.BacklogItemIds.Count; i++)
            {
                var backlogItem = await _context.BacklogItems.FindAsync(request.BacklogItemIds[i]);
                if (backlogItem != null && backlogItem.ProjectId == sprint.ProjectId)
                {
                    backlogItem.SprintId = sprint.Id;
                    backlogItem.Status = BacklogItemStatus.ToDo;
                    backlogItem.SprintPriority = i;
                }
            }

            var totalStoryPoints = await _context.BacklogItems
                .Where(bi => bi.SprintId == sprint.Id)
                .SumAsync(bi => bi.StoryPoints ?? 0);

            sprint.IsActive = true;
            sprint.Status = SprintStatus.Active;
            sprint.CommittedStoryPoints = (int)totalStoryPoints;

            await _context.SaveChangesAsync();

            await _notificationService.NotifyProjectMembersAsync(
                sprint.ProjectId,
                "Спринт начат",
                $"Спринт '{sprint.Name}' начат. Запланировано {request.BacklogItemIds.Count} задач",
                "Info",
                $"/sprints/{sprint.Id}",
                sprint.Id,
                "Sprint"
            );

            var response = await MapToSprintResponse(sprint);
            return ApiResponse<SprintResponse>.Ok(response, "Спринт запущен");
        }

        public async Task<ApiResponse<SprintResponse>> CompleteSprintAsync(CompleteSprintRequest request)
        {
            var sprint = await _context.Sprints
                .Include(s => s.BacklogItems)
                .FirstOrDefaultAsync(s => s.Id == request.SprintId);

            if (sprint == null)
            {
                return ApiResponse<SprintResponse>.Fail("Спринт не найден");
            }

            if (!sprint.IsActive)
            {
                return ApiResponse<SprintResponse>.Fail("Спринт не активен");
            }

            var completedStoryPoints = sprint.BacklogItems
                .Where(bi => bi.Status == BacklogItemStatus.Done)
                .Sum(bi => bi.StoryPoints ?? 0);

            foreach (var item in sprint.BacklogItems.Where(bi => bi.Status != BacklogItemStatus.Done))
            {
                item.SprintId = null;
                item.Status = BacklogItemStatus.Backlog;
                item.SprintPriority = null;
            }

            sprint.IsActive = false;
            sprint.Status = SprintStatus.Completed;
            sprint.CompletedStoryPoints = (int)completedStoryPoints;
            sprint.CompletedAt = DateTime.UtcNow;
            sprint.ReviewNotes = request.ReviewNotes;
            sprint.RetrospectiveNotes = request.RetrospectiveNotes;

            var velocity = new SprintVelocity
            {
                Id = Guid.NewGuid(),
                SprintId = sprint.Id,
                TotalStoryPoints = sprint.CommittedStoryPoints ?? 0,
                CompletedStoryPoints = completedStoryPoints,
                CommittedTasksCount = sprint.BacklogItems.Count,
                CompletedTasksCount = sprint.BacklogItems.Count(bi => bi.Status == BacklogItemStatus.Done),
                Velocity = completedStoryPoints,
                CalculatedAt = DateTime.UtcNow
            };

            _context.SprintVelocities.Add(velocity);
            await _context.SaveChangesAsync();

            await _notificationService.NotifyProjectMembersAsync(
                sprint.ProjectId,
                "Спринт завершен",
                $"Спринт '{sprint.Name}' завершен. Выполнено {completedStoryPoints} из {sprint.CommittedStoryPoints} Story Points",
                "Success",
                $"/sprints/{sprint.Id}",
                sprint.Id,
                "Sprint"
            );

            var response = await MapToSprintResponse(sprint);
            return ApiResponse<SprintResponse>.Ok(response, "Спринт завершен");
        }

        public async Task<ApiResponse> CancelSprintAsync(Guid sprintId)
        {
            var sprint = await _context.Sprints
                .Include(s => s.BacklogItems)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return ApiResponse.Fail("Спринт не найден");
            }

            foreach (var item in sprint.BacklogItems)
            {
                item.SprintId = null;
                item.Status = BacklogItemStatus.Backlog;
                item.SprintPriority = null;
            }

            sprint.IsActive = false;
            sprint.Status = SprintStatus.Cancelled;

            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Спринт отменен");
        }

        public async Task<ApiResponse<SprintBoardResponse>> GetSprintBoardAsync(Guid sprintId)
        {
            var sprint = await _context.Sprints
                .Include(s => s.BacklogItems)
                    .ThenInclude(bi => bi.Assignee)
                .Include(s => s.BacklogItems)
                    .ThenInclude(bi => bi.SubTasks)
                .Include(s => s.BacklogItems)
                    .ThenInclude(bi => bi.Blockers)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return ApiResponse<SprintBoardResponse>.Fail("Спринт не найден");
            }

            var boardTasks = sprint.BacklogItems.Select(bi => new BacklogItemBoardResponse
            {
                Id = bi.Id,
                Title = bi.Title,
                Type = bi.Type.ToString(),
                Status = bi.Status.ToString(),
                Priority = bi.Priority,
                StoryPoints = bi.StoryPoints,
                EstimatedHours = bi.EstimatedHours,
                Assignee = bi.Assignee != null ? new UserBriefResponse
                {
                    Id = bi.Assignee.Id,
                    FullName = bi.Assignee.FullName,
                    Username = bi.Assignee.Username
                } : null,
                HasBlockers = bi.Blockers.Any(b => b.Status == BlockerStatus.Active),
                SubTasksCount = bi.SubTasks.Count,
                CompletedSubTasksCount = bi.SubTasks.Count(st => st.Status == SubTaskStatus.Done)
            }).ToList();

            var metricsResult = await GetSprintMetricsAsync(sprintId);
            var burndownResult = await GetBurndownChartAsync(sprintId);

            var response = new SprintBoardResponse
            {
                Id = sprint.Id,
                Name = sprint.Name,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                IsActive = sprint.IsActive,
                Status = sprint.Status.ToString(),
                CreatedAt = sprint.CreatedAt,
                TotalTasksCount = sprint.BacklogItems.Count,
                CompletedTasksCount = sprint.BacklogItems.Count(bi => bi.Status == BacklogItemStatus.Done),
                TotalStoryPoints = sprint.BacklogItems.Sum(bi => bi.StoryPoints),
                CompletedStoryPoints = sprint.BacklogItems.Where(bi => bi.Status == BacklogItemStatus.Done).Sum(bi => bi.StoryPoints),
                CompletionPercentage = sprint.BacklogItems.Any()
                    ? (double)sprint.BacklogItems.Count(bi => bi.Status == BacklogItemStatus.Done) / sprint.BacklogItems.Count * 100
                    : 0,
                DaysRemaining = sprint.IsActive ? (sprint.EndDate - DateTime.UtcNow.Date).Days : 0,
                Tasks = boardTasks,
                Metrics = metricsResult.Data ?? new SprintMetrics(),
                BurndownData = burndownResult.Data ?? new List<BurndownPoint>()
            };

            return ApiResponse<SprintBoardResponse>.Ok(response);
        }

        public async Task<ApiResponse> UpdateTaskStatusAsync(Guid taskId, string newStatus)
        {
            var task = await _context.BacklogItems.FindAsync(taskId);
            if (task == null)
            {
                return ApiResponse.Fail("Задача не найдена");
            }

            if (!Enum.TryParse<BacklogItemStatus>(newStatus, true, out var status))
            {
                return ApiResponse.Fail("Неверный статус");
            }

            var oldStatus = task.Status;
            task.Status = status;

            if (status == BacklogItemStatus.InProgress && !task.StartedAt.HasValue)
            {
                task.StartedAt = DateTime.UtcNow;
            }

            if (status == BacklogItemStatus.Done && !task.CompletedAt.HasValue)
            {
                task.CompletedAt = DateTime.UtcNow;
            }

            task.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _context.ActivityLogs.Add(new ActivityLog
            {
                ProjectId = task.ProjectId,
                UserId = task.AssigneeId ?? Guid.Empty,
                ActionType = ActionType.TaskStatusChanged,
                EntityType = "BacklogItem",
                EntityId = task.Id,
                Description = $"Статус задачи '{task.Title}' изменен с {oldStatus} на {status}",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Статус задачи обновлен");
        }

        public async Task<ApiResponse> MoveToSprintAsync(MoveToSprintRequest request)
        {
            var sprint = await _context.Sprints.FindAsync(request.SprintId);
            if (sprint == null)
            {
                return ApiResponse.Fail("Спринт не найден");
            }

            for (int i = 0; i < request.BacklogItemIds.Count; i++)
            {
                var item = await _context.BacklogItems.FindAsync(request.BacklogItemIds[i]);
                if (item != null && item.ProjectId == sprint.ProjectId)
                {
                    item.SprintId = sprint.Id;
                    item.Status = BacklogItemStatus.ToDo;
                    item.SprintPriority = i;
                }
            }

            await _context.SaveChangesAsync();
            return ApiResponse.Ok("Задачи перемещены в спринт");
        }

        public async Task<ApiResponse> MoveToBacklogAsync(Guid backlogItemId)
        {
            var item = await _context.BacklogItems.FindAsync(backlogItemId);
            if (item == null)
            {
                return ApiResponse.Fail("Задача не найдена");
            }

            item.SprintId = null;
            item.Status = BacklogItemStatus.Backlog;
            item.SprintPriority = null;
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Задача перемещена в бэклог");
        }

        public async Task<ApiResponse<SprintMetrics>> GetSprintMetricsAsync(Guid sprintId)
        {
            var sprint = await _context.Sprints
                .Include(s => s.BacklogItems)
                    .ThenInclude(bi => bi.Assignee)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return ApiResponse<SprintMetrics>.Fail("Спринт не найден");
            }

            var tasksByStatus = sprint.BacklogItems
                .GroupBy(bi => bi.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var tasksByAssignee = sprint.BacklogItems
                .Where(bi => bi.Assignee != null)
                .GroupBy(bi => bi.Assignee!.FullName)
                .ToDictionary(g => g.Key, g => g.Count());

            var burndownResult = await GetBurndownChartAsync(sprintId);

            var metrics = new SprintMetrics
            {
                Velocity = sprint.CompletedStoryPoints ?? 0,
                TasksByStatus = tasksByStatus,
                TasksByAssignee = tasksByAssignee,
                BurndownData = burndownResult.Data ?? new List<BurndownPoint>()
            };

            return ApiResponse<SprintMetrics>.Ok(metrics);
        }

        public async Task<ApiResponse<List<BurndownPoint>>> GetBurndownChartAsync(Guid sprintId)
        {
            var sprint = await _context.Sprints
                .Include(s => s.BacklogItems)
                    .ThenInclude(bi => bi.SubTasks)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return ApiResponse<List<BurndownPoint>>.Fail("Спринт не найден");
            }

            var burndownPoints = new List<BurndownPoint>();

            // Рассчитываем общее количество часов
            var totalHours = sprint.BacklogItems
                .Sum(bi => bi.EstimatedHours ?? 0) +
                sprint.BacklogItems
                    .SelectMany(bi => bi.SubTasks)
                    .Sum(st => st.EstimatedHours ?? 0);

            // Если нет задач с оценкой времени, возвращаем пустой список
            if (totalHours == 0)
            {
                return ApiResponse<List<BurndownPoint>>.Ok(new List<BurndownPoint>(), "Нет данных для графика");
            }

            var days = (sprint.EndDate - sprint.StartDate).Days;
            if (days <= 0) days = 1;

            var dailyIdealBurn = totalHours / days;

            // Создаем словарь выполненных часов по дням
            var completedHoursPerDay = new Dictionary<DateTime, decimal>();

            foreach (var task in sprint.BacklogItems.Where(bi => bi.Status == BacklogItemStatus.Done && bi.CompletedAt.HasValue))
            {
                var completedDate = task.CompletedAt!.Value.Date;
                var taskHours = (task.EstimatedHours ?? 0) + task.SubTasks.Sum(st => st.EstimatedHours ?? 0);

                if (!completedHoursPerDay.ContainsKey(completedDate))
                    completedHoursPerDay[completedDate] = 0;

                completedHoursPerDay[completedDate] += taskHours;
            }

            var remaining = totalHours;
            var currentDate = sprint.StartDate.Date;

            while (currentDate <= sprint.EndDate.Date)
            {
                // Вычитаем выполненные в этот день часы
                if (completedHoursPerDay.ContainsKey(currentDate))
                {
                    remaining -= completedHoursPerDay[currentDate];
                }

                var idealRemaining = Math.Max(0, totalHours - dailyIdealBurn * (currentDate - sprint.StartDate.Date).Days);
                var actualRemaining = Math.Max(0, remaining);

                burndownPoints.Add(new BurndownPoint
                {
                    Date = currentDate,
                    RemainingHours = actualRemaining,
                    IdealRemainingHours = idealRemaining,
                    RemainingStoryPoints = 0
                });

                currentDate = currentDate.AddDays(1);
            }

            return ApiResponse<List<BurndownPoint>>.Ok(burndownPoints);
        }

        public async Task<ApiResponse> SaveReviewNotesAsync(Guid sprintId, string notes)
        {
            var sprint = await _context.Sprints.FindAsync(sprintId);
            if (sprint == null)
            {
                return ApiResponse.Fail("Спринт не найден");
            }

            sprint.ReviewNotes = notes;
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Заметки Sprint Review сохранены");
        }

        public async Task<ApiResponse> SaveRetrospectiveNotesAsync(Guid sprintId, string notes)
        {
            var sprint = await _context.Sprints.FindAsync(sprintId);
            if (sprint == null)
            {
                return ApiResponse.Fail("Спринт не найден");
            }

            sprint.RetrospectiveNotes = notes;
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Заметки ретроспективы сохранены");
        }

        public async Task<ApiResponse<List<SprintVelocityHistory>>> GetSprintHistoryAsync(Guid projectId, int count = 5)
        {
            var history = await _context.SprintVelocities
                .Include(sv => sv.Sprint)
                .Where(sv => sv.Sprint.ProjectId == projectId)
                .OrderByDescending(sv => sv.Sprint.EndDate)
                .Take(count)
                .Select(sv => new SprintVelocityHistory
                {
                    SprintId = sv.SprintId,
                    SprintName = sv.Sprint.Name,
                    EndDate = sv.Sprint.EndDate,
                    TotalStoryPoints = sv.TotalStoryPoints,
                    CompletedStoryPoints = sv.CompletedStoryPoints,
                    Velocity = sv.Velocity,
                    CommittedTasks = sv.CommittedTasksCount,
                    CompletedTasks = sv.CompletedTasksCount
                })
                .ToListAsync();

            return ApiResponse<List<SprintVelocityHistory>>.Ok(history);
        }

        #region Private Methods

        private async Task<SprintResponse> MapToSprintResponse(Sprint sprint)
        {
            var backlogItems = await _context.BacklogItems
                .Where(bi => bi.SprintId == sprint.Id)
                .ToListAsync();

            var totalTasks = backlogItems.Count;
            var completedTasks = backlogItems.Count(bi => bi.Status == BacklogItemStatus.Done);
            var totalStoryPoints = backlogItems.Sum(bi => bi.StoryPoints ?? 0);
            var completedStoryPoints = backlogItems
                .Where(bi => bi.Status == BacklogItemStatus.Done)
                .Sum(bi => bi.StoryPoints ?? 0);
            var daysRemaining = sprint.IsActive && sprint.EndDate > DateTime.UtcNow.Date
                ? (sprint.EndDate - DateTime.UtcNow.Date).Days
                : 0;

            return new SprintResponse
            {
                Id = sprint.Id,
                Name = sprint.Name,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                IsActive = sprint.IsActive,
                Status = sprint.Status.ToString(),
                CreatedAt = sprint.CreatedAt,
                TotalTasksCount = totalTasks,
                CompletedTasksCount = completedTasks,
                TotalStoryPoints = totalStoryPoints,
                CompletedStoryPoints = completedStoryPoints,
                CompletionPercentage = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0,
                DaysRemaining = daysRemaining,
                CommittedStoryPoints = sprint.CommittedStoryPoints,
                CompletedStoryPointsModel = sprint.CompletedStoryPoints,
                CompletedAt = sprint.CompletedAt,
                ReviewNotes = sprint.ReviewNotes,
                RetrospectiveNotes = sprint.RetrospectiveNotes
            };
        }

        #endregion
    }
}