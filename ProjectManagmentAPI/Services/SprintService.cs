using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Models;
using ProjectManagementAPI.Enums;


namespace ProjectManagementAPI.Services
{
    public class SprintService : BaseService, ISprintService
    {
        private readonly ContextDb _context;
        private readonly INotificationService _notificationService;

        public SprintService(
            ContextDb context,
            INotificationService notificationService,
            ILogger<SprintService> logger) : base(logger)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<SprintResponse>> CreateSprintAsync(CreateSprintRequest request)
        {
            try
            {
                var project = await _context.Projects.FindAsync(request.ProjectId);
                if (project == null)
                {
                    return ApiResponse<SprintResponse>.Fail("Проект не найден");
                }

                if (request.EndDate < request.StartDate)
                {
                    return ApiResponse<SprintResponse>.Fail("Дата окончания не может быть раньше даты начала");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания спринта");
                return ApiResponse<SprintResponse>.Fail("Произошла ошибка при создании спринта");
            }
        }

        public async Task<ApiResponse<SprintResponse>> GetSprintByIdAsync(Guid sprintId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения спринта {SprintId}", sprintId);
                return ApiResponse<SprintResponse>.Fail("Произошла ошибка при получении данных спринта");
            }
        }

        public async Task<ApiResponse<List<SprintBriefResponse>>> GetProjectSprintsAsync(Guid projectId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения спринтов проекта {ProjectId}", projectId);
                return ApiResponse<List<SprintBriefResponse>>.Fail("Произошла ошибка при получении списка спринтов");
            }
        }

        public async Task<ApiResponse<SprintResponse>> UpdateSprintAsync(Guid sprintId, UpdateSprintRequest request)
        {
            try
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
                {
                    if (request.EndDate.Value < sprint.StartDate)
                    {
                        return ApiResponse<SprintResponse>.Fail("Дата окончания не может быть раньше даты начала");
                    }
                    sprint.EndDate = request.EndDate.Value;
                }

                if (request.Status != null && Enum.TryParse<SprintStatus>(request.Status, true, out var status))
                    sprint.Status = status;

                await _context.SaveChangesAsync();

                var response = await MapToSprintResponse(sprint);
                return ApiResponse<SprintResponse>.Ok(response, "Спринт обновлен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления спринта {SprintId}", sprintId);
                return ApiResponse<SprintResponse>.Fail("Произошла ошибка при обновлении спринта");
            }
        }

        public async Task<ApiResponse> DeleteSprintAsync(Guid sprintId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления спринта {SprintId}", sprintId);
                return ApiResponse.Fail("Произошла ошибка при удалении спринта");
            }
        }

        public async Task<ApiResponse<SprintResponse>> StartSprintAsync(StartSprintRequest request)
        {
            try
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

                if (sprint.Status != SprintStatus.Planned)
                {
                    _logger.LogWarning($"Спринт {sprint.Id} имеет статус {sprint.Status}, ожидался Planned");
                    return ApiResponse<SprintResponse>.Fail($"Спринт можно запустить только в статусе Planned. Текущий статус: {sprint.Status}");
                }

                if (request.BacklogItemIds != null && request.BacklogItemIds.Any())
                {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка запуска спринта {SprintId}", request.SprintId);
                return ApiResponse<SprintResponse>.Fail($"Произошла ошибка при запуске спринта: {ex.Message}");
            }
        }

        public async Task<ApiResponse<SprintResponse>> CompleteSprintAsync(CompleteSprintRequest request)
        {
            try
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

                _logger.LogInformation($"Завершение спринта {sprint.Id}. Всего задач: {sprint.BacklogItems.Count}");

                var completedStoryPoints = sprint.BacklogItems
                    .Where(bi => bi.Status == BacklogItemStatus.Done)
                    .Sum(bi => bi.StoryPoints ?? 0);

                _logger.LogInformation($"Выполнено Story Points: {completedStoryPoints}");

                var incompleteTasks = sprint.BacklogItems.Where(bi => bi.Status != BacklogItemStatus.Done).ToList();
                _logger.LogInformation($"Незавершенных задач: {incompleteTasks.Count}");

                foreach (var item in incompleteTasks)
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
                    $"Спринт '{sprint.Name}' завершен. Выполнено {completedStoryPoints} из {sprint.CommittedStoryPoints} Story Points. Velocity команды: {completedStoryPoints}",
                    "Success",
                    $"/sprints/{sprint.Id}",
                    sprint.Id,
                    "Sprint"
                );

                var response = await MapToSprintResponse(sprint);
                return ApiResponse<SprintResponse>.Ok(response, "Спринт завершен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка завершения спринта {SprintId}", request.SprintId);
                return ApiResponse<SprintResponse>.Fail($"Произошла ошибка при завершении спринта: {ex.Message}");
            }
        }

        public async Task<ApiResponse> CancelSprintAsync(Guid sprintId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отмены спринта {SprintId}", sprintId);
                return ApiResponse.Fail("Произошла ошибка при отмене спринта");
            }
        }

        public async Task<ApiResponse<SprintBoardResponse>> GetSprintBoardAsync(Guid sprintId)
        {
            try
            {
                var sprint = await _context.Sprints
                    .Include(s => s.Project)
                    .FirstOrDefaultAsync(s => s.Id == sprintId);

                if (sprint == null)
                {
                    return ApiResponse<SprintBoardResponse>.Fail("Спринт не найден");
                }

                var boardTasks = await _context.BacklogItems
                    .Where(bi => bi.SprintId == sprintId)
                    .Select(bi => new BacklogItemBoardResponse
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
                        ActiveBlockers = bi.Blockers
                            .Where(b => b.Status == BlockerStatus.Active)
                            .Select(b => new BlockerResponse
                            {
                                Id = b.Id,
                                Description = b.Description,
                                Severity = b.Severity.ToString(),
                                Status = b.Status.ToString(),
                                CreatedAt = b.CreatedAt
                            }).ToList(),
                        SubTasksCount = bi.SubTasks.Count,
                        CompletedSubTasksCount = bi.SubTasks.Count(st => st.Status == SubTaskStatus.Done)
                    })
                    .ToListAsync();

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
                    ProjectId = sprint.ProjectId,
                    ProjectOwnerId = sprint.Project.OwnerId,
                    TotalTasksCount = boardTasks.Count,
                    CompletedTasksCount = boardTasks.Count(t => t.Status == BacklogItemStatus.Done.ToString()),
                    TotalStoryPoints = boardTasks.Sum(t => t.StoryPoints) ?? 0,
                    CompletedStoryPoints = boardTasks.Where(t => t.Status == BacklogItemStatus.Done.ToString()).Sum(t => t.StoryPoints) ?? 0,
                    CompletionPercentage = boardTasks.Any()
                        ? (double)boardTasks.Count(t => t.Status == BacklogItemStatus.Done.ToString()) / boardTasks.Count * 100
                        : 0,
                    DaysRemaining = sprint.IsActive ? (sprint.EndDate - DateTime.UtcNow.Date).Days : 0,
                    Tasks = boardTasks,
                    Metrics = new SprintMetrics(),
                    BurndownData = new List<BurndownPoint>()
                };

                return ApiResponse<SprintBoardResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения доски спринта {SprintId}", sprintId);
                return ApiResponse<SprintBoardResponse>.Fail("Ошибка при получении данных доски спринта");
            }
        }

        public async Task<ApiResponse> UpdateTaskStatusAsync(Guid taskId, string newStatus)
        {
            try
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

                _logger.LogInformation($"Изменение статуса задачи {taskId}: {task.Status} -> {status}");

                // Проверка для Done
                if (status == BacklogItemStatus.Done)
                {
                    var subTasks = await _context.SubTasks
                        .Where(st => st.BacklogItemId == taskId)
                        .ToListAsync();

                    var incompleteSubtasks = subTasks.Any(st => st.Status != SubTaskStatus.Done);
                    _logger.LogInformation($"Подзадачи: {subTasks.Count}, незавершённые: {incompleteSubtasks}");

                    if (subTasks.Any() && incompleteSubtasks)
                    {
                        _logger.LogWarning($"Нельзя завершить задачу {taskId}: есть незавершённые подзадачи");
                        return ApiResponse.Fail("Нельзя завершить задачу, пока не выполнены все подзадачи");
                    }

                    var activeBlockers = await _context.Blockers
                        .AnyAsync(b => b.BacklogItemId == taskId && b.Status == BlockerStatus.Active);
                    _logger.LogInformation($"Активные блокеры: {activeBlockers}");

                    if (activeBlockers)
                    {
                        _logger.LogWarning($"Нельзя завершить задачу {taskId}: есть активные блокеры");
                        return ApiResponse.Fail("Нельзя завершить задачу, пока есть активные блокеры");
                    }

                    task.CompletedAt = DateTime.UtcNow;
                }

                var oldStatus = task.Status;
                task.Status = status;

                if (status == BacklogItemStatus.InProgress && !task.StartedAt.HasValue)
                {
                    task.StartedAt = DateTime.UtcNow;
                }

                task.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _context.ActivityLogs.Add(new ActivityLog
                {
                    ProjectId = task.ProjectId,
                    UserId = task.AssigneeId ?? task.CreatedById,
                    ActionType = ActionType.TaskStatusChanged,
                    EntityType = "BacklogItem",
                    EntityId = task.Id,
                    Description = $"Статус задачи '{task.Title}' изменен с {oldStatus} на {status}",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                return ApiResponse.Ok("Статус задачи обновлен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления статуса задачи {TaskId}", taskId);
                return ApiResponse.Fail("Произошла ошибка при обновлении статуса задачи");
            }
        }

        public async Task<ApiResponse> MoveToSprintAsync(MoveToSprintRequest request)
        {
            try
            {
                var sprint = await _context.Sprints.FindAsync(request.SprintId);
                if (sprint == null)
                {
                    return ApiResponse.Fail("Спринт не найден");
                }

                if (sprint.IsActive)
                {
                    return ApiResponse.Fail("Нельзя перемещать задачи в активный спринт. Сначала завершите спринт.");
                }

                var backlogItems = await _context.BacklogItems
                    .Where(bi => request.BacklogItemIds.Contains(bi.Id))
                    .ToListAsync();

                if (backlogItems.Count == 0)
                {
                    return ApiResponse.Fail("Задачи не найдены");
                }

                foreach (var item in backlogItems)
                {
                    item.SprintId = sprint.Id;
                    item.Status = BacklogItemStatus.ToDo;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Перемещено {backlogItems.Count} задач в спринт {sprint.Id}");

                return ApiResponse.Ok("Задачи перемещены в спринт");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка перемещения задач в спринт");
                return ApiResponse.Fail($"Произошла ошибка: {ex.Message}");
            }
        }

        public async Task<ApiResponse> MoveToBacklogAsync(Guid backlogItemId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка перемещения задачи в бэклог {BacklogItemId}", backlogItemId);
                return ApiResponse.Fail("Произошла ошибка при перемещении задачи");
            }
        }

        public async Task<ApiResponse<SprintMetrics>> GetSprintMetricsAsync(Guid sprintId)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения метрик спринта {SprintId}", sprintId);
                return ApiResponse<SprintMetrics>.Fail("Произошла ошибка при получении метрик");
            }
        }

        public async Task<ApiResponse<List<BurndownPoint>>> GetBurndownChartAsync(Guid sprintId)
        {
            try
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

                var totalHours = sprint.BacklogItems
                    .Sum(bi => bi.EstimatedHours ?? 0) +
                    sprint.BacklogItems
                        .SelectMany(bi => bi.SubTasks)
                        .Sum(st => st.EstimatedHours ?? 0);

                if (totalHours == 0)
                {
                    return ApiResponse<List<BurndownPoint>>.Ok(new List<BurndownPoint>(), "Нет данных для графика");
                }

                var days = (sprint.EndDate - sprint.StartDate).Days;
                if (days <= 0) days = 1;

                var dailyIdealBurn = totalHours / days;

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения графика сгорания спринта {SprintId}", sprintId);
                return ApiResponse<List<BurndownPoint>>.Fail("Произошла ошибка при получении данных графика");
            }
        }

        public async Task<ApiResponse> SaveReviewNotesAsync(Guid sprintId, string notes)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения заметок Review спринта {SprintId}", sprintId);
                return ApiResponse.Fail("Произошла ошибка при сохранении заметок");
            }
        }

        public async Task<ApiResponse> SaveRetrospectiveNotesAsync(Guid sprintId, string notes)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сохранения заметок ретроспективы спринта {SprintId}", sprintId);
                return ApiResponse.Fail("Произошла ошибка при сохранении заметок");
            }
        }

        public async Task<ApiResponse<List<SprintVelocityHistory>>> GetSprintHistoryAsync(Guid projectId, int count = 5)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения истории спринтов проекта {ProjectId}", projectId);
                return ApiResponse<List<SprintVelocityHistory>>.Fail("Произошла ошибка при получении истории спринтов");
            }
        }

        #region Private Methods

        private async Task<SprintResponse> MapToSprintResponse(Sprint sprint)
        {
            var stats = await _context.BacklogItems
                .Where(bi => bi.SprintId == sprint.Id)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalTasks = g.Count(),
                    CompletedTasks = g.Count(bi => bi.Status == BacklogItemStatus.Done),
                    TotalStoryPoints = g.Sum(bi => bi.StoryPoints ?? 0),
                    CompletedStoryPoints = g.Sum(bi => bi.Status == BacklogItemStatus.Done ? bi.StoryPoints ?? 0 : 0)
                })
                .FirstOrDefaultAsync();

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
                ProjectId = sprint.ProjectId,
                TotalTasksCount = stats?.TotalTasks ?? 0,
                CompletedTasksCount = stats?.CompletedTasks ?? 0,
                TotalStoryPoints = stats?.TotalStoryPoints ?? 0,
                CompletedStoryPoints = stats?.CompletedStoryPoints ?? 0,
                CompletionPercentage = stats?.TotalTasks > 0 ? (double)(stats?.CompletedTasks ?? 0) / (stats?.TotalTasks ?? 1) * 100 : 0,
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