using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.Models;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.DTO.Requests;
using System.Text.Json;

namespace ProjectManagementAPI.Services
{
    public class BacklogService : BaseService, IBacklogService
    {
        private readonly ContextDb _context;
        private readonly INotificationService _notificationService;

        public BacklogService(ContextDb context, INotificationService notificationService, ILogger<UserService> logger) : base(logger)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<BacklogItemResponse>> CreateBacklogItemAsync(CreateBacklogItemRequest request)
        {
            var project = await _context.Projects.FindAsync(request.ProjectId);
            if (project == null)
            {
                return ApiResponse<BacklogItemResponse>.Fail("Проект не найден");
            }

            var maxOrder = await _context.BacklogItems
                .Where(bi => bi.ProjectId == request.ProjectId && bi.SprintId == null)
                .MaxAsync(bi => (int?)bi.OrderInBacklog) ?? 0;

            var backlogItem = new BacklogItem
            {
                Id = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                Type = request.Type,
                Title = request.Title,
                Description = request.Description,
                AcceptanceCriteria = request.AcceptanceCriteria,
                Priority = request.Priority ?? maxOrder + 1,
                StoryPoints = request.StoryPoints,
                EstimatedHours = request.EstimatedHours,
                Status = BacklogItemStatus.Backlog,
                AssigneeId = request.AssigneeId,
                CreatedById = request.CreatedById,
                OrderInBacklog = maxOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.BacklogItems.Add(backlogItem);
            await _context.SaveChangesAsync();

            var createdItem = await _context.BacklogItems
                .Include(bi => bi.Assignee)
                .Include(bi => bi.CreatedBy)
                .FirstOrDefaultAsync(bi => bi.Id == backlogItem.Id);

            var response = await MapToBacklogItemResponse(createdItem ?? backlogItem);
            return ApiResponse<BacklogItemResponse>.Ok(response, "Задача создана");
        }

        public async Task<ApiResponse<BacklogItemResponse>> GetBacklogItemByIdAsync(Guid id)
        {
            var backlogItem = await _context.BacklogItems
                .Include(bi => bi.Assignee)
                .Include(bi => bi.CreatedBy)
                .Include(bi => bi.Sprint)
                .FirstOrDefaultAsync(bi => bi.Id == id);

            if (backlogItem == null)
            {
                return ApiResponse<BacklogItemResponse>.Fail("Задача не найдена");
            }

            var response = await MapToBacklogItemResponse(backlogItem);
            return ApiResponse<BacklogItemResponse>.Ok(response);
        }

        public async Task<ApiResponse<PagedResult<BacklogItemResponse>>> GetProjectBacklogAsync(Guid projectId, PagedRequest request)
        {
            var baseQuery = _context.BacklogItems
                .Where(bi => bi.ProjectId == projectId);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                baseQuery = baseQuery.Where(bi =>
                    bi.Title.Contains(request.SearchTerm) ||
                    (bi.Description != null && bi.Description.Contains(request.SearchTerm)));
            }

            var totalCount = await baseQuery.CountAsync();

            var items = await baseQuery
                .Include(bi => bi.Assignee)
                .Include(bi => bi.CreatedBy)
                .Include(bi => bi.Sprint)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var itemIds = items.Select(i => i.Id).ToList();
            var stats = await _context.BacklogItems
                .Where(bi => itemIds.Contains(bi.Id))
                .Select(bi => new
                {
                    bi.Id,
                    SubTasksCount = bi.SubTasks.Count,
                    CompletedSubTasksCount = bi.SubTasks.Count(st => st.Status == SubTaskStatus.Done),
                    CommentsCount = bi.Comments.Count,
                    AttachmentsCount = bi.Attachments.Count,
                    ActiveBlockers = bi.Blockers
                        .Where(b => b.Status == BlockerStatus.Active)
                        .Select(b => new BlockerResponse
                        {
                            Id = b.Id,
                            Description = b.Description,
                            Severity = b.Severity.ToString(),
                            Status = b.Status.ToString(),
                            CreatedAt = b.CreatedAt
                        }).ToList()
                })
                .ToDictionaryAsync(x => x.Id);

            var responses = items.Select(item =>
            {
                var s = stats.GetValueOrDefault(item.Id);
                return new BacklogItemResponse
                {
                    Id = item.Id,
                    Type = item.Type.ToString(),
                    Title = item.Title,
                    Description = item.Description,
                    AcceptanceCriteria = item.AcceptanceCriteria,
                    Priority = item.Priority,
                    StoryPoints = item.StoryPoints,
                    EstimatedHours = item.EstimatedHours,
                    Status = item.Status.ToString(),
                    Assignee = item.Assignee != null ? new UserBriefResponse { Id = item.Assignee.Id, FullName = item.Assignee.FullName, Username = item.Assignee.Username } : null,
                    CreatedBy = item.CreatedBy != null ? new UserBriefResponse { Id = item.CreatedBy.Id, FullName = item.CreatedBy.FullName, Username = item.CreatedBy.Username } : new UserBriefResponse { Id = Guid.Empty, FullName = "Неизвестный", Username = "unknown" },
                    Sprint = item.Sprint != null ? new SprintBriefResponse { Id = item.Sprint.Id, Name = item.Sprint.Name, Status = item.Sprint.Status.ToString(), StartDate = item.Sprint.StartDate, EndDate = item.Sprint.EndDate, IsActive = item.Sprint.IsActive } : null,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    CompletedAt = item.CompletedAt,
                    SprintPriority = item.SprintPriority,
                    StartedAt = item.StartedAt,
                    ActualHours = item.ActualHours,
                    SubTasksCount = s?.SubTasksCount ?? 0,
                    CompletedSubTasksCount = s?.CompletedSubTasksCount ?? 0,
                    CommentsCount = s?.CommentsCount ?? 0,
                    AttachmentsCount = s?.AttachmentsCount ?? 0,
                    ActiveBlockers = s?.ActiveBlockers ?? new(),
                    Efficiency = item.EstimatedHours.HasValue && item.ActualHours.HasValue && item.ActualHours.Value > 0
                        ? Math.Round((double)item.EstimatedHours.Value / item.ActualHours.Value * 100, 1) : null
                };
            }).ToList();

            return ApiResponse<PagedResult<BacklogItemResponse>>.Ok(new PagedResult<BacklogItemResponse>
            {
                Items = responses,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });
        }

        public async Task<ApiResponse<BacklogItemResponse>> UpdateBacklogItemAsync(Guid id, UpdateBacklogItemRequest request)
        {
            var backlogItem = await _context.BacklogItems.Include(bi => bi.Project).FirstOrDefaultAsync(bi => bi.Id == id);

            if (backlogItem == null)
            {
                return ApiResponse<BacklogItemResponse>.Fail("Задача не найдена");
            }

            var oldAssigneeId = backlogItem.AssigneeId;
            var project = backlogItem.Project;

            var oldValues = new
            {
                backlogItem.Title,
                backlogItem.Description,
                backlogItem.Type,
                backlogItem.Priority,
                backlogItem.StoryPoints,
                backlogItem.EstimatedHours,
                backlogItem.Status,
                backlogItem.AssigneeId
            };

            if (request.Title != null)
                backlogItem.Title = request.Title;

            if (request.Description != null)
                backlogItem.Description = request.Description;

            if (request.AcceptanceCriteria != null)
                backlogItem.AcceptanceCriteria = request.AcceptanceCriteria;

            if (request.Type.HasValue)
                backlogItem.Type = request.Type.Value;

            if (request.Priority.HasValue)
                backlogItem.Priority = request.Priority.Value;

            if (request.StoryPoints.HasValue)
                backlogItem.StoryPoints = request.StoryPoints.Value;

            if (request.EstimatedHours.HasValue)
                backlogItem.EstimatedHours = request.EstimatedHours.Value;

            if (request.Status.HasValue)
                backlogItem.Status = request.Status.Value;

            if (request.AssigneeId.HasValue)
                backlogItem.AssigneeId = request.AssigneeId.Value == Guid.Empty ? null : request.AssigneeId.Value;

            backlogItem.UpdatedAt = DateTime.UtcNow;

            if (request.AssigneeId.HasValue && request.AssigneeId.Value != Guid.Empty && oldAssigneeId != request.AssigneeId.Value)
            {
                await _notificationService.CreateNotificationAsync(
                    request.AssigneeId.Value,
                    "Назначена задача",
                    $"Вам назначена задача '{backlogItem.Title}' в проекте '{project.Name}'",
                    "Info",
                    $"/backlog/{backlogItem.Id}",
                    backlogItem.Id,
                    "BacklogItem"
                );
            }

            await _context.SaveChangesAsync();

            var response = await MapToBacklogItemResponse(backlogItem);
            return ApiResponse<BacklogItemResponse>.Ok(response, "Задача обновлена");
        }

        public async Task<ApiResponse> DeleteBacklogItemAsync(Guid id)
        {
            var backlogItem = await _context.BacklogItems
                .Include(bi => bi.SubTasks)
                .Include(bi => bi.Comments)
                .Include(bi => bi.Attachments)
                .FirstOrDefaultAsync(bi => bi.Id == id);

            if (backlogItem == null)
            {
                return ApiResponse.Fail("Задача не найдена");
            }

            _context.BacklogItems.Remove(backlogItem);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Задача удалена");
        }

        public async Task<ApiResponse<BacklogItemResponse>> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request)
        {
            var backlogItem = await _context.BacklogItems.FindAsync(id);
            if (backlogItem == null)
            {
                return ApiResponse<BacklogItemResponse>.Fail("Задача не найдена");
            }

            var oldStatus = backlogItem.Status;
            backlogItem.Status = request.NewStatus;

            if (request.NewStatus == BacklogItemStatus.InProgress && !backlogItem.StartedAt.HasValue)
            {
                backlogItem.StartedAt = DateTime.UtcNow;
            }

            if (request.NewStatus == BacklogItemStatus.Done && !backlogItem.CompletedAt.HasValue)
            {
                backlogItem.CompletedAt = DateTime.UtcNow;
            }

            backlogItem.UpdatedAt = DateTime.UtcNow;

            if (request.NewStatus == BacklogItemStatus.Done && oldStatus != BacklogItemStatus.Done)
            {
                if (backlogItem.CreatedById != backlogItem.AssigneeId)
                {
                    await _notificationService.CreateNotificationAsync(
                        backlogItem.CreatedById,
                        "Задача выполнена",
                        $"Задача '{backlogItem.Title}' выполнена {(backlogItem.Assignee != null ? $"пользователем {backlogItem.Assignee.FullName}" : "")}",
                        "Success",
                        $"/backlog/{backlogItem.Id}",
                        backlogItem.Id,
                        "BacklogItem"
                    );
                }

                var scrumMasters = await _context.ProjectMembers
                    .Where(pm => pm.ProjectId == backlogItem.ProjectId && pm.RoleInProject == ProjectRole.ScrumMaster)
                    .Select(pm => pm.UserId)
                    .ToListAsync();

                foreach (var scrumMasterId in scrumMasters)
                {
                    if (scrumMasterId != backlogItem.AssigneeId && scrumMasterId != backlogItem.CreatedById)
                    {
                        await _notificationService.CreateNotificationAsync(
                            scrumMasterId,
                            "Задача выполнена",
                            $"Задача '{backlogItem.Title}' в проекте выполнена",
                            "Success",
                            $"/backlog/{backlogItem.Id}",
                            backlogItem.Id,
                            "BacklogItem"
                        );
                    }
                }
            }

            _context.ActivityLogs.Add(new ActivityLog
            {
                ProjectId = backlogItem.ProjectId,
                UserId = request.UserId != Guid.Empty ? request.UserId : backlogItem.AssigneeId ?? backlogItem.CreatedById,
                ActionType = ActionType.TaskStatusChanged,
                EntityType = "BacklogItem",
                EntityId = backlogItem.Id,
                Description = $"Статус задачи '{backlogItem.Title}' изменен с {oldStatus} на {request.NewStatus}",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var response = await MapToBacklogItemResponse(backlogItem);
            return ApiResponse<BacklogItemResponse>.Ok(response, "Статус изменен");
        }

        public async Task<ApiResponse> ReorderBacklogAsync(ReorderBacklogRequest request)
        {
            var ids = request.Items.Select(i => i.Id).ToList();
            var items = await _context.BacklogItems.Where(bi => ids.Contains(bi.Id)).ToListAsync();
            foreach (var item in items)
            {
                var newOrder = request.Items.First(i => i.Id == item.Id).NewOrder;
                item.OrderInBacklog = newOrder;
            }
            await _context.SaveChangesAsync();
            return ApiResponse.Ok("Порядок бэклога обновлен");
        }

        public async Task<ApiResponse<CommentResponse>> AddCommentAsync(Guid backlogItemId, AddCommentRequest request, Guid userId)
        {
            try
            {
                var backlogItem = await _context.BacklogItems.FindAsync(backlogItemId);
                if (backlogItem == null)
                    return ApiResponse<CommentResponse>.Fail("Задача не найдена");

                var comment = new Comment
                {
                    Id = Guid.NewGuid(),
                    BacklogItemId = backlogItemId,
                    UserId = userId, // ← получаем из JWT, а не из запроса
                    Content = request.Content,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);

                var response = new CommentResponse
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt,
                    User = new UserBriefResponse
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Username = user.Username
                    }
                };

                return ApiResponse<CommentResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления комментария");
                return ApiResponse<CommentResponse>.Fail("Ошибка сервера");
            }
        }

        public async Task<ApiResponse<CommentResponse>> UpdateCommentAsync(Guid commentId, UpdateCommentRequest request, Guid userId)
        {
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return ApiResponse<CommentResponse>.Fail("Комментарий не найден");
            }

            if (comment.UserId != userId)
            {
                return ApiResponse<CommentResponse>.Fail("Вы не можете редактировать чужой комментарий");
            }

            comment.Content = request.Content;
            comment.UpdatedAt = DateTime.UtcNow;
            comment.IsEdited = true;
            await _context.SaveChangesAsync();

            var response = new CommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                User = new UserBriefResponse
                {
                    Id = comment.User.Id,
                    FullName = comment.User.FullName,
                    Username = comment.User.Username
                },
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsEdited = comment.IsEdited
            };

            return ApiResponse<CommentResponse>.Ok(response, "Комментарий обновлен");
        }

        public async Task<ApiResponse> DeleteCommentAsync(Guid commentId, Guid userId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return ApiResponse.Fail("Комментарий не найден");
            }

            if (comment.UserId != userId)
            {
                return ApiResponse.Fail("Вы не можете удалить чужой комментарий");
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Комментарий удален");
        }

        public async Task<ApiResponse<AttachmentResponse>> UploadAttachmentAsync(Guid backlogItemId, UploadAttachmentRequest request)
        {
            var backlogItem = await _context.BacklogItems.FindAsync(backlogItemId);
            if (backlogItem == null)
                return ApiResponse<AttachmentResponse>.Fail("Задача не найдена");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Attachments");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileId = Guid.NewGuid();
            var fileExtension = Path.GetExtension(request.FileName);
            var storedFileName = $"{fileId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            await File.WriteAllBytesAsync(filePath, request.FileContent);

            var attachment = new Attachment
            {
                Id = fileId,
                BacklogItemId = backlogItemId,
                UploadedById = request.UploadedById,
                FileName = request.FileName,
                FileUrl = $"/api/backlog/attachments/{fileId}/download",
                FileSize = request.FileContent.Length,
                MimeType = request.MimeType,
                UploadedAt = DateTime.UtcNow
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            var response = new AttachmentResponse
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                FileUrl = attachment.FileUrl,
                FileSize = attachment.FileSize,
                MimeType = attachment.MimeType,
                UploadedBy = new UserBriefResponse
                {
                    Id = request.UploadedById,
                    FullName = request.UploadedByName ?? "Неизвестный"
                },
                UploadedAt = attachment.UploadedAt
            };

            return ApiResponse<AttachmentResponse>.Ok(response, "Файл загружен");
        }

        public async Task<byte[]> DownloadAttachmentAsync(Guid attachmentId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null)
            {
                _logger.LogWarning("Запись в БД не найдена для attachmentId: {Id}", attachmentId);
                return Array.Empty<byte>();
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Attachments");
            var fileExtension = Path.GetExtension(attachment.FileName);
            var storedFileName = $"{attachmentId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Файл не найден на диске: {Path}. Искали по имени: {FileName}", filePath, storedFileName);
                return Array.Empty<byte>();
            }

            _logger.LogInformation("Файл найден, размер: {Size} байт", new FileInfo(filePath).Length);
            return await File.ReadAllBytesAsync(filePath);
        }

        public async Task<ApiResponse> DeleteAttachmentAsync(Guid attachmentId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null)
            {
                return ApiResponse.Fail("Вложение не найдено");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Attachments");
            var fileExtension = Path.GetExtension(attachment.FileName);
            var storedFileName = $"{attachmentId}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, storedFileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Вложение удалено");
        }

        public async Task<ApiResponse<BlockerResponse>> AddBlockerAsync(Guid backlogItemId, string description, string severity)
        {
            var backlogItem = await _context.BacklogItems.FindAsync(backlogItemId);
            if (backlogItem == null)
            {
                return ApiResponse<BlockerResponse>.Fail("Задача не найдена");
            }

            if (!Enum.TryParse<BlockerSeverity>(severity, true, out var blockerSeverity))
            {
                blockerSeverity = BlockerSeverity.Medium;
            }

            var blocker = new Blocker
            {
                Id = Guid.NewGuid(),
                BacklogItemId = backlogItemId,
                Description = description,
                Severity = blockerSeverity,
                Status = BlockerStatus.Active,
                ReportedById = backlogItem.AssigneeId ?? Guid.Empty,
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

        public async Task<ApiResponse> ResolveBlockerAsync(Guid blockerId, string resolutionNote)
        {
            var blocker = await _context.Blockers.FindAsync(blockerId);
            if (blocker == null)
            {
                return ApiResponse.Fail("Блокер не найден");
            }

            blocker.Status = BlockerStatus.Resolved;
            blocker.ResolvedAt = DateTime.UtcNow;
            blocker.ResolutionNote = resolutionNote;
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Блокер разрешен");
        }

        public async Task<ApiResponse<BacklogItemDetailResponse>> GetBacklogItemDetailAsync(Guid id)
        {
            var backlogItem = await _context.BacklogItems
                .Include(bi => bi.Assignee)
                .Include(bi => bi.CreatedBy)
                .Include(bi => bi.Sprint)
                .FirstOrDefaultAsync(bi => bi.Id == id);

            if (backlogItem == null)
                return ApiResponse<BacklogItemDetailResponse>.Fail("Задача не найдена");

            // Параллельная загрузка
            var subTasksTask = _context.SubTasks.Include(st => st.Assignee)
                .Where(st => st.BacklogItemId == id).OrderBy(st => st.OrderInParent).ToListAsync();
            var commentsTask = _context.Comments.Include(c => c.User)
                .Where(c => c.BacklogItemId == id).OrderBy(c => c.CreatedAt).Take(50).ToListAsync();
            var attachmentsTask = _context.Attachments.Include(a => a.UploadedBy)
                .Where(a => a.BacklogItemId == id).ToListAsync();
            var blockersTask = _context.Blockers
                .Where(b => b.BacklogItemId == id && b.Status == BlockerStatus.Active).ToListAsync();
            var logsTask = _context.ActivityLogs
                .Where(al => al.EntityId == id && al.EntityType == "BacklogItem")
                .OrderByDescending(al => al.CreatedAt).Take(20)
                .Select(al => new ActivityLogResponse { Id = al.Id, ActionType = al.ActionType.ToString(), Description = al.Description, CreatedAt = al.CreatedAt })
                .ToListAsync();

            await Task.WhenAll(subTasksTask, commentsTask, attachmentsTask, blockersTask, logsTask);

            var subTasks = subTasksTask.Result;
            var comments = commentsTask.Result;
            var attachments = attachmentsTask.Result;
            var blockers = blockersTask.Result;
            var activityLogs = logsTask.Result;

            return ApiResponse<BacklogItemDetailResponse>.Ok(new BacklogItemDetailResponse
            {
                Id = backlogItem.Id,
                Type = backlogItem.Type.ToString(),
                Title = backlogItem.Title,
                Description = backlogItem.Description,
                AcceptanceCriteria = backlogItem.AcceptanceCriteria,
                Priority = backlogItem.Priority,
                StoryPoints = backlogItem.StoryPoints,
                EstimatedHours = backlogItem.EstimatedHours,
                Status = backlogItem.Status.ToString(),
                Assignee = backlogItem.Assignee != null ? new UserBriefResponse { Id = backlogItem.Assignee.Id, FullName = backlogItem.Assignee.FullName, Username = backlogItem.Assignee.Username } : null,
                CreatedBy = new UserBriefResponse { Id = backlogItem.CreatedBy.Id, FullName = backlogItem.CreatedBy.FullName, Username = backlogItem.CreatedBy.Username },
                Sprint = backlogItem.Sprint != null ? new SprintBriefResponse { Id = backlogItem.Sprint.Id, Name = backlogItem.Sprint.Name, Status = backlogItem.Sprint.Status.ToString(), StartDate = backlogItem.Sprint.StartDate, EndDate = backlogItem.Sprint.EndDate, IsActive = backlogItem.Sprint.IsActive } : null,
                CreatedAt = backlogItem.CreatedAt,
                UpdatedAt = backlogItem.UpdatedAt,
                CompletedAt = backlogItem.CompletedAt,
                SprintPriority = backlogItem.SprintPriority,
                StartedAt = backlogItem.StartedAt,
                ActualHours = backlogItem.ActualHours,
                SubTasks = subTasks.Select(st => new SubTaskResponse { /* ... */ }).ToList(),
                Comments = comments.Select(c => new CommentResponse { /* ... */ }).ToList(),
                Attachments = attachments.Select(a => new AttachmentResponse { /* ... */ }).ToList(),
                ActiveBlockers = blockers.Select(b => new BlockerResponse { /* ... */ }).ToList(),
                ActivityHistory = activityLogs,
                SubTasksCount = subTasks.Count,
                CompletedSubTasksCount = subTasks.Count(st => st.Status == SubTaskStatus.Done),
                CommentsCount = comments.Count,
                AttachmentsCount = attachments.Count
            });
        }

        #region Private Methods

        private async Task<BacklogItemResponse> MapToBacklogItemResponse(BacklogItem backlogItem)
        {
            if (backlogItem.Id == Guid.Empty)
                return CreateEmptyResponse();

            var stats = await _context.BacklogItems
                .Where(bi => bi.Id == backlogItem.Id)
                .Select(bi => new
                {
                    SubTasksCount = bi.SubTasks.Count,
                    CompletedSubTasksCount = bi.SubTasks.Count(st => st.Status == SubTaskStatus.Done),
                    CommentsCount = bi.Comments.Count,
                    AttachmentsCount = bi.Attachments.Count,
                    ActiveBlockers = bi.Blockers
                        .Where(b => b.Status == BlockerStatus.Active)
                        .Select(b => new BlockerResponse
                        {
                            Id = b.Id,
                            Description = b.Description,
                            Severity = b.Severity.ToString(),
                            Status = b.Status.ToString(),
                            CreatedAt = b.CreatedAt
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return new BacklogItemResponse
            {
                Id = backlogItem.Id,
                Type = backlogItem.Type.ToString(),
                Title = backlogItem.Title,
                Description = backlogItem.Description,
                AcceptanceCriteria = backlogItem.AcceptanceCriteria,
                Priority = backlogItem.Priority,
                StoryPoints = backlogItem.StoryPoints,
                EstimatedHours = backlogItem.EstimatedHours,
                Status = backlogItem.Status.ToString(),
                Assignee = backlogItem.Assignee != null ? new UserBriefResponse
                {
                    Id = backlogItem.Assignee.Id,
                    FullName = backlogItem.Assignee.FullName,
                    Username = backlogItem.Assignee.Username
                } : null,
                CreatedBy = backlogItem.CreatedBy != null ? new UserBriefResponse
                {
                    Id = backlogItem.CreatedBy.Id,
                    FullName = backlogItem.CreatedBy.FullName,
                    Username = backlogItem.CreatedBy.Username
                } : new UserBriefResponse { Id = Guid.Empty, FullName = "Неизвестный", Username = "unknown" },
                Sprint = backlogItem.Sprint != null ? new SprintBriefResponse
                {
                    Id = backlogItem.Sprint.Id,
                    Name = backlogItem.Sprint.Name,
                    Status = backlogItem.Sprint.Status.ToString(),
                    StartDate = backlogItem.Sprint.StartDate,
                    EndDate = backlogItem.Sprint.EndDate,
                    IsActive = backlogItem.Sprint.IsActive
                } : null,
                CreatedAt = backlogItem.CreatedAt,
                UpdatedAt = backlogItem.UpdatedAt,
                CompletedAt = backlogItem.CompletedAt,
                SprintPriority = backlogItem.SprintPriority,
                StartedAt = backlogItem.StartedAt,
                ActualHours = backlogItem.ActualHours,
                SubTasksCount = stats?.SubTasksCount ?? 0,
                CompletedSubTasksCount = stats?.CompletedSubTasksCount ?? 0,
                CommentsCount = stats?.CommentsCount ?? 0,
                AttachmentsCount = stats?.AttachmentsCount ?? 0,
                ActiveBlockers = stats?.ActiveBlockers ?? new(),
                Efficiency = backlogItem.EstimatedHours.HasValue && backlogItem.ActualHours.HasValue && backlogItem.ActualHours.Value > 0
                    ? Math.Round((double)backlogItem.EstimatedHours.Value / backlogItem.ActualHours.Value * 100, 1) : null
            };
        }

        private BacklogItemResponse CreateEmptyResponse()
        {
            return new BacklogItemResponse
            {
                CreatedBy = new UserBriefResponse { Id = Guid.Empty, FullName = "Неизвестный", Username = "unknown" }
            };
        }

        #endregion
    }
}