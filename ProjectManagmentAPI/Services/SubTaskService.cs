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
    public class SubTaskService : ISubTaskService
    {
        private readonly ContextDb _context;
        private readonly INotificationService _notificationService;

        public SubTaskService(ContextDb context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<SubTaskResponse>> CreateSubTaskAsync(CreateSubTaskRequest request)
        {
            var backlogItem = await _context.BacklogItems.FindAsync(request.BacklogItemId);
            if (backlogItem == null)
            {
                return ApiResponse<SubTaskResponse>.Fail("Родительская задача не найдена");
            }

            var maxOrder = await _context.SubTasks
                .Where(st => st.BacklogItemId == request.BacklogItemId)
                .MaxAsync(st => (int?)st.OrderInParent) ?? 0;

            var subTask = new SubTask
            {
                Id = Guid.NewGuid(),
                BacklogItemId = request.BacklogItemId,
                Title = request.Title,
                Description = request.Description,
                EstimatedHours = request.EstimatedHours,
                Status = SubTaskStatus.ToDo,
                AssigneeId = request.AssigneeId,
                OrderInParent = maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.SubTasks.Add(subTask);
            await _context.SaveChangesAsync();

            _context.ActivityLogs.Add(new ActivityLog
            {
                ProjectId = backlogItem.ProjectId,
                UserId = request.CreatedById,
                ActionType = ActionType.SubTaskCreated,
                EntityType = "SubTask",
                EntityId = subTask.Id,
                Description = $"Создана подзадача '{subTask.Title}' для задачи '{backlogItem.Title}'",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var response = await MapToSubTaskResponse(subTask);
            return ApiResponse<SubTaskResponse>.Ok(response, "Подзадача создана");
        }

        public async Task<ApiResponse<SubTaskResponse>> GetSubTaskByIdAsync(Guid subTaskId)
        {
            var subTask = await _context.SubTasks
                .Include(st => st.Assignee)
                .Include(st => st.BacklogItem)
                .FirstOrDefaultAsync(st => st.Id == subTaskId);

            if (subTask == null)
            {
                return ApiResponse<SubTaskResponse>.Fail("Подзадача не найдена");
            }

            var response = await MapToSubTaskResponse(subTask);
            return ApiResponse<SubTaskResponse>.Ok(response);
        }

        public async Task<ApiResponse<List<SubTaskResponse>>> GetBacklogItemSubTasksAsync(Guid backlogItemId)
        {
            var subTasks = await _context.SubTasks
                .Where(st => st.BacklogItemId == backlogItemId)
                .Include(st => st.Assignee)
                .OrderBy(st => st.OrderInParent)
                .ToListAsync();

            var responses = new List<SubTaskResponse>();
            foreach (var subTask in subTasks)
            {
                responses.Add(await MapToSubTaskResponse(subTask));
            }

            return ApiResponse<List<SubTaskResponse>>.Ok(responses);
        }

        public async Task<ApiResponse<SubTaskResponse>> UpdateSubTaskAsync(Guid subTaskId, UpdateSubTaskRequest request)
        {
            var subTask = await _context.SubTasks.FindAsync(subTaskId);
            if (subTask == null)
            {
                return ApiResponse<SubTaskResponse>.Fail("Подзадача не найдена");
            }

            if (request.Title != null)
                subTask.Title = request.Title;

            if (request.Description != null)
                subTask.Description = request.Description;

            if (request.EstimatedHours.HasValue)
                subTask.EstimatedHours = request.EstimatedHours.Value;

            if (request.ActualHours.HasValue)
                subTask.ActualHours = request.ActualHours.Value;

            if (request.Status != null && Enum.TryParse<SubTaskStatus>(request.Status, true, out var status))
                subTask.Status = status;

            if (request.AssigneeId.HasValue)
                subTask.AssigneeId = request.AssigneeId;

            if (request.OrderInParent.HasValue)
                subTask.OrderInParent = request.OrderInParent.Value;

            subTask.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = await MapToSubTaskResponse(subTask);
            return ApiResponse<SubTaskResponse>.Ok(response, "Подзадача обновлена");
        }

        public async Task<ApiResponse> DeleteSubTaskAsync(Guid subTaskId)
        {
            var subTask = await _context.SubTasks.FindAsync(subTaskId);
            if (subTask == null)
            {
                return ApiResponse.Fail("Подзадача не найдена");
            }

            _context.SubTasks.Remove(subTask);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Подзадача удалена");
        }

        public async Task<ApiResponse<SubTaskResponse>> StartSubTaskAsync(StartSubTaskRequest request)
        {
            var subTask = await _context.SubTasks.FindAsync(request.SubTaskId);
            if (subTask == null)
            {
                return ApiResponse<SubTaskResponse>.Fail("Подзадача не найдена");
            }

            if (subTask.Status != SubTaskStatus.ToDo)
            {
                return ApiResponse<SubTaskResponse>.Fail("Подзадача уже в работе или завершена");
            }

            subTask.Status = SubTaskStatus.InProgress;
            subTask.StartedAt = DateTime.UtcNow;
            subTask.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var backlogItem = await _context.BacklogItems
                .Include(bi => bi.SubTasks)
                .FirstOrDefaultAsync(bi => bi.Id == subTask.BacklogItemId);

            if (backlogItem != null && backlogItem.Status != BacklogItemStatus.InProgress)
            {
                backlogItem.Status = BacklogItemStatus.InProgress;
                backlogItem.StartedAt ??= DateTime.UtcNow;
                backlogItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var response = await MapToSubTaskResponse(subTask);
            return ApiResponse<SubTaskResponse>.Ok(response, "Работа над подзадачей начата");
        }

        public async Task<ApiResponse<SubTaskResponse>> CompleteSubTaskAsync(CompleteSubTaskRequest request)
        {
            var subTask = await _context.SubTasks.FindAsync(request.SubTaskId);
            if (subTask == null)
            {
                return ApiResponse<SubTaskResponse>.Fail("Подзадача не найдена");
            }

            if (subTask.Status == SubTaskStatus.Done)
            {
                return ApiResponse<SubTaskResponse>.Fail("Подзадача уже завершена");
            }

            subTask.Status = SubTaskStatus.Done;
            subTask.CompletedAt = DateTime.UtcNow;

            if (request.ActualHours.HasValue)
                subTask.ActualHours = request.ActualHours.Value;

            subTask.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var backlogItem = await _context.BacklogItems
                .Include(bi => bi.SubTasks)
                .FirstOrDefaultAsync(bi => bi.Id == subTask.BacklogItemId);

            if (backlogItem != null && backlogItem.SubTasks.All(st => st.Status == SubTaskStatus.Done))
            {
                backlogItem.Status = BacklogItemStatus.Done;
                backlogItem.CompletedAt = DateTime.UtcNow;
                backlogItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var response = await MapToSubTaskResponse(subTask);
            return ApiResponse<SubTaskResponse>.Ok(response, "Подзадача завершена");
        }

        public async Task<ApiResponse<SubTaskResponse>> ChangeStatusAsync(Guid subTaskId, ChangeSubTaskStatusRequest request)
        {
            var subTask = await _context.SubTasks.FindAsync(subTaskId);
            if (subTask == null)
            {
                return ApiResponse<SubTaskResponse>.Fail("Подзадача не найдена");
            }

            if (!Enum.TryParse<SubTaskStatus>(request.NewStatus, true, out var status))
            {
                return ApiResponse<SubTaskResponse>.Fail("Неверный статус");
            }

            var oldStatus = subTask.Status;
            subTask.Status = status;

            if (status == SubTaskStatus.InProgress && !subTask.StartedAt.HasValue)
            {
                subTask.StartedAt = DateTime.UtcNow;
            }

            if (status == SubTaskStatus.Done && !subTask.CompletedAt.HasValue)
            {
                subTask.CompletedAt = DateTime.UtcNow;
            }

            subTask.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var response = await MapToSubTaskResponse(subTask);
            return ApiResponse<SubTaskResponse>.Ok(response, $"Статус изменен с {oldStatus} на {status}");
        }

        public async Task<ApiResponse> ReorderSubTasksAsync(ReorderSubTasksRequest request)
        {
            foreach (var item in request.Items)
            {
                var subTask = await _context.SubTasks.FindAsync(item.Id);
                if (subTask != null)
                {
                    subTask.OrderInParent = item.NewOrder;
                }
            }

            await _context.SaveChangesAsync();
            return ApiResponse.Ok("Порядок подзадач обновлен");
        }

        public async Task<ApiResponse<SubTaskStatisticsResponse>> GetSubTaskStatisticsAsync(Guid backlogItemId)
        {
            var subTasks = await _context.SubTasks
                .Where(st => st.BacklogItemId == backlogItemId)
                .ToListAsync();

            var statistics = new SubTaskStatisticsResponse
            {
                TotalCount = subTasks.Count,
                CompletedCount = subTasks.Count(st => st.Status == SubTaskStatus.Done),
                InProgressCount = subTasks.Count(st => st.Status == SubTaskStatus.InProgress),
                TodoCount = subTasks.Count(st => st.Status == SubTaskStatus.ToDo),
                TotalEstimatedHours = subTasks.Sum(st => st.EstimatedHours ?? 0),
                TotalActualHours = subTasks.Sum(st => st.ActualHours ?? 0),
                CompletionPercentage = subTasks.Any()
                    ? (double)subTasks.Count(st => st.Status == SubTaskStatus.Done) / subTasks.Count * 100
                    : 0,
                TasksByAssignee = subTasks
                    .Where(st => st.AssigneeId.HasValue)
                    .GroupBy(st => st.AssigneeId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return ApiResponse<SubTaskStatisticsResponse>.Ok(statistics);
        }

        public async Task<ApiResponse<BlockerResponse>> AddBlockerToSubTaskAsync(Guid subTaskId, string description, string severity)
        {
            var subTask = await _context.SubTasks.FindAsync(subTaskId);
            if (subTask == null)
            {
                return ApiResponse<BlockerResponse>.Fail("Подзадача не найдена");
            }

            if (!Enum.TryParse<BlockerSeverity>(severity, true, out var blockerSeverity))
            {
                blockerSeverity = BlockerSeverity.Medium;
            }

            var blocker = new Blocker
            {
                Id = Guid.NewGuid(),
                SubTaskId = subTaskId,
                Description = description,
                Severity = blockerSeverity,
                Status = BlockerStatus.Active,
                ReportedById = subTask.AssigneeId ?? Guid.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Blockers.Add(blocker);
            await _context.SaveChangesAsync();

            var response = new BlockerResponse
            {
                Id = blocker.Id,
                Description = blocker.Description,
                Severity = blocker.Severity.ToString(),
                Status = blocker.Status.ToString(),
                ReportedBy = new UserBriefResponse { Id = blocker.ReportedById },
                CreatedAt = blocker.CreatedAt
            };

            return ApiResponse<BlockerResponse>.Ok(response, "Блокер добавлен");
        }

        #region Private Methods

        private async Task<SubTaskResponse> MapToSubTaskResponse(SubTask subTask)
        {
            var activeBlockers = await _context.Blockers
                .Where(b => b.SubTaskId == subTask.Id && b.Status == BlockerStatus.Active)
                .Select(b => new BlockerResponse
                {
                    Id = b.Id,
                    Description = b.Description,
                    Severity = b.Severity.ToString(),
                    Status = b.Status.ToString(),
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return new SubTaskResponse
            {
                Id = subTask.Id,
                BacklogItemId = subTask.BacklogItemId,
                Title = subTask.Title,
                Description = subTask.Description,
                EstimatedHours = subTask.EstimatedHours,
                ActualHours = subTask.ActualHours,
                Status = subTask.Status.ToString(),
                Assignee = subTask.Assignee != null ? new UserBriefResponse
                {
                    Id = subTask.Assignee.Id,
                    FullName = subTask.Assignee.FullName,
                    Username = subTask.Assignee.Username
                } : null,
                OrderInParent = subTask.OrderInParent,
                StartedAt = subTask.StartedAt,
                CompletedAt = subTask.CompletedAt,
                CreatedAt = subTask.CreatedAt,
                UpdatedAt = subTask.UpdatedAt,
                IsOverdue = subTask.CompletedAt == null && subTask.StartedAt != null &&
                            (DateTime.UtcNow - subTask.StartedAt.Value).TotalDays > 3,
                HasBlockers = activeBlockers.Any(),
                ActiveBlockers = activeBlockers,
                ActualMinutes = subTask.ActualHours.HasValue ? (int?)(subTask.ActualHours.Value * 60) : null,
                Efficiency = subTask.EstimatedHours.HasValue && subTask.ActualHours.HasValue
                    ? Math.Round((double)subTask.EstimatedHours.Value / (double)subTask.ActualHours.Value * 100, 1)
                    : null
            };
        }

        #endregion
    }
}