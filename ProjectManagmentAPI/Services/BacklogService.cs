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
            var query = _context.BacklogItems
                .Where(bi => bi.ProjectId == projectId && bi.SprintId == null)
                .Include(bi => bi.Assignee)
                .Include(bi => bi.CreatedBy)
                .OrderBy(bi => bi.OrderInBacklog);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = (IOrderedQueryable<BacklogItem>)query.Where(bi =>
                    bi.Title.Contains(request.SearchTerm) ||
                    (bi.Description != null && bi.Description.Contains(request.SearchTerm)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var responses = new List<BacklogItemResponse>();
            foreach (var item in items)
            {
                responses.Add(await MapToBacklogItemResponse(item));
            }

            var result = new PagedResult<BacklogItemResponse>
            {
                Items = responses,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return ApiResponse<PagedResult<BacklogItemResponse>>.Ok(result);
        }

        public async Task<ApiResponse<BacklogItemResponse>> UpdateBacklogItemAsync(Guid id, UpdateBacklogItemRequest request)
        {
            var backlogItem = await _context.BacklogItems.FindAsync(id);
            if (backlogItem == null)
            {
                return ApiResponse<BacklogItemResponse>.Fail("Задача не найдена");
            }

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
                backlogItem.AssigneeId = request.AssigneeId;

            backlogItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _context.ActivityLogs.Add(new ActivityLog
            {
                ProjectId = backlogItem.ProjectId,
                UserId = request.UserId,
                ActionType = ActionType.TaskUpdated,
                EntityType = "BacklogItem",
                EntityId = backlogItem.Id,
                OldValue = JsonSerializer.Serialize(oldValues),
                NewValue = JsonSerializer.Serialize(new
                {
                    backlogItem.Title,
                    backlogItem.Description,
                    backlogItem.Type,
                    backlogItem.Priority,
                    backlogItem.StoryPoints,
                    backlogItem.EstimatedHours,
                    backlogItem.Status,
                    backlogItem.AssigneeId
                }),
                Description = $"Задача '{backlogItem.Title}' обновлена",
                CreatedAt = DateTime.UtcNow
            });

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
            await _context.SaveChangesAsync();

            _context.ActivityLogs.Add(new ActivityLog
            {
                ProjectId = backlogItem.ProjectId,
                UserId = request.UserId,
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
            foreach (var item in request.Items)
            {
                var backlogItem = await _context.BacklogItems.FindAsync(item.Id);
                if (backlogItem != null)
                {
                    backlogItem.OrderInBacklog = item.NewOrder;
                }
            }

            await _context.SaveChangesAsync();
            return ApiResponse.Ok("Порядок бэклога обновлен");
        }

        public async Task<ApiResponse<CommentResponse>> AddCommentAsync(Guid backlogItemId, AddCommentRequest request)
        {
            var backlogItem = await _context.BacklogItems.FindAsync(backlogItemId);
            if (backlogItem == null)
            {
                return ApiResponse<CommentResponse>.Fail("Задача не найдена");
            }

            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                BacklogItemId = backlogItemId,
                UserId = request.UserId,
                Content = request.Content,
                MentionedUsers = request.MentionedUserIds != null && request.MentionedUserIds.Any()
                    ? JsonSerializer.Serialize(request.MentionedUserIds)
                    : null,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            if (request.MentionedUserIds != null)
            {
                foreach (var mentionedUserId in request.MentionedUserIds)
                {
                    await _notificationService.CreateNotificationAsync(
                        mentionedUserId,
                        "Вас упомянули в комментарии",
                        $"{request.UserName} упомянул вас в комментарии к задаче '{backlogItem.Title}'",
                        "Info",
                        $"/backlog/{backlogItemId}",
                        backlogItemId,
                        "BacklogItem"
                    );
                }
            }

            var response = new CommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                User = new UserBriefResponse
                {
                    Id = request.UserId,
                    FullName = request.UserName,
                    Username = request.UserLogin
                },
                MentionedUsers = request.MentionedUserIds?.Select(id => new UserBriefResponse { Id = id }).ToList() ?? new List<UserBriefResponse>(),
                CreatedAt = comment.CreatedAt,
                IsEdited = false
            };

            return ApiResponse<CommentResponse>.Ok(response, "Комментарий добавлен");
        }

        public async Task<ApiResponse<CommentResponse>> UpdateCommentAsync(Guid commentId, UpdateCommentRequest request)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return ApiResponse<CommentResponse>.Fail("Комментарий не найден");
            }

            comment.Content = request.Content;
            comment.UpdatedAt = DateTime.UtcNow;
            comment.IsEdited = true;
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(comment.UserId);
            var response = new CommentResponse
            {
                Id = comment.Id,
                Content = comment.Content,
                User = new UserBriefResponse
                {
                    Id = user!.Id,
                    FullName = user.FullName,
                    Username = user.Username
                },
                CreatedAt = comment.CreatedAt,
                UpdatedAt = comment.UpdatedAt,
                IsEdited = comment.IsEdited
            };

            return ApiResponse<CommentResponse>.Ok(response, "Комментарий обновлен");
        }

        public async Task<ApiResponse> DeleteCommentAsync(Guid commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
            {
                return ApiResponse.Fail("Комментарий не найден");
            }

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Комментарий удален");
        }

        public async Task<ApiResponse<AttachmentResponse>> UploadAttachmentAsync(Guid backlogItemId, UploadAttachmentRequest request)
        {
            var backlogItem = await _context.BacklogItems.FindAsync(backlogItemId);
            if (backlogItem == null)
            {
                return ApiResponse<AttachmentResponse>.Fail("Задача не найдена");
            }

            var attachment = new Attachment
            {
                Id = Guid.NewGuid(),
                BacklogItemId = backlogItemId,
                UploadedById = request.UploadedById,
                FileName = request.FileName,
                FileUrl = $"/attachments/{Guid.NewGuid()}/{request.FileName}",
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
                    FullName = request.UploadedByName
                },
                UploadedAt = attachment.UploadedAt
            };

            return ApiResponse<AttachmentResponse>.Ok(response, "Файл загружен");
        }

        public async Task<ApiResponse> DeleteAttachmentAsync(Guid attachmentId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null)
            {
                return ApiResponse.Fail("Вложение не найдено");
            }

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Вложение удалено");
        }

        public async Task<byte[]> DownloadAttachmentAsync(Guid attachmentId)
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null)
            {
                return Array.Empty<byte>();
            }

            return Array.Empty<byte>();
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
                .Include(bi => bi.SubTasks)
                    .ThenInclude(st => st.Assignee)
                .Include(bi => bi.Comments)
                    .ThenInclude(c => c.User)
                .Include(bi => bi.Attachments)
                    .ThenInclude(a => a.UploadedBy)
                .Include(bi => bi.Blockers)
                .FirstOrDefaultAsync(bi => bi.Id == id);

            if (backlogItem == null)
            {
                return ApiResponse<BacklogItemDetailResponse>.Fail("Задача не найдена");
            }

            var activityLogs = await _context.ActivityLogs
                .Where(al => al.EntityId == id && al.EntityType == "BacklogItem")
                .OrderByDescending(al => al.CreatedAt)
                .Take(50)
                .ToListAsync();

            var response = new BacklogItemDetailResponse
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
                CreatedBy = new UserBriefResponse
                {
                    Id = backlogItem.CreatedBy.Id,
                    FullName = backlogItem.CreatedBy.FullName,
                    Username = backlogItem.CreatedBy.Username
                },
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
                SubTasks = backlogItem.SubTasks.Select(st => new SubTaskResponse
                {
                    Id = st.Id,
                    Title = st.Title,
                    Description = st.Description,
                    EstimatedHours = st.EstimatedHours,
                    ActualHours = st.ActualHours,
                    Status = st.Status.ToString(),
                    Assignee = st.Assignee != null ? new UserBriefResponse
                    {
                        Id = st.Assignee.Id,
                        FullName = st.Assignee.FullName,
                        Username = st.Assignee.Username
                    } : null,
                    OrderInParent = st.OrderInParent,
                    StartedAt = st.StartedAt,
                    CompletedAt = st.CompletedAt,
                    CreatedAt = st.CreatedAt,
                    UpdatedAt = st.UpdatedAt,
                    IsOverdue = st.CompletedAt == null && st.StartedAt != null &&
                                (DateTime.UtcNow - st.StartedAt.Value).TotalDays > 3,
                    HasBlockers = false,
                    ActualMinutes = st.ActualHours.HasValue ? (int?)(st.ActualHours.Value * 60) : null,
                    Efficiency = st.EstimatedHours.HasValue && st.ActualHours.HasValue
                        ? Math.Round((double)st.EstimatedHours.Value / (double)st.ActualHours.Value * 100, 1)
                        : null
                }).ToList(),
                Comments = backlogItem.Comments.Select(c => new CommentResponse
                {
                    Id = c.Id,
                    Content = c.Content,
                    User = new UserBriefResponse
                    {
                        Id = c.User.Id,
                        FullName = c.User.FullName,
                        Username = c.User.Username
                    },
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsEdited = c.IsEdited
                }).ToList(),
                Attachments = backlogItem.Attachments.Select(a => new AttachmentResponse
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileSize = a.FileSize,
                    MimeType = a.MimeType,
                    UploadedBy = new UserBriefResponse
                    {
                        Id = a.UploadedBy.Id,
                        FullName = a.UploadedBy.FullName,
                        Username = a.UploadedBy.Username
                    },
                    UploadedAt = a.UploadedAt
                }).ToList(),
                ActiveBlockers = backlogItem.Blockers
                    .Where(b => b.Status == BlockerStatus.Active)
                    .Select(b => new BlockerResponse
                    {
                        Id = b.Id,
                        Description = b.Description,
                        Severity = b.Severity.ToString(),
                        Status = b.Status.ToString(),
                        CreatedAt = b.CreatedAt
                    }).ToList(),
                ActivityHistory = activityLogs.Select(al => new ActivityLogResponse
                {
                    Id = al.Id,
                    ActionType = al.ActionType.ToString(),
                    Description = al.Description,
                    CreatedAt = al.CreatedAt
                }).ToList(),
                SubTasksCount = backlogItem.SubTasks.Count,
                CompletedSubTasksCount = backlogItem.SubTasks.Count(st => st.Status == SubTaskStatus.Done),
                CommentsCount = backlogItem.Comments.Count,
                AttachmentsCount = backlogItem.Attachments.Count
            };

            return ApiResponse<BacklogItemDetailResponse>.Ok(response);
        }

        #region Private Methods

        private async Task<BacklogItemResponse> MapToBacklogItemResponse(BacklogItem backlogItem)
        {
            try
            {
                var subTasks = new List<SubTask>();
                var commentsCount = 0;
                var attachmentsCount = 0;
                var activeBlockers = new List<BlockerResponse>();

                if (backlogItem.Id != Guid.Empty)
                {
                    subTasks = await _context.SubTasks
                        .Where(st => st.BacklogItemId == backlogItem.Id)
                        .ToListAsync();

                    commentsCount = await _context.Comments
                        .CountAsync(c => c.BacklogItemId == backlogItem.Id);

                    attachmentsCount = await _context.Attachments
                        .CountAsync(a => a.BacklogItemId == backlogItem.Id);

                    activeBlockers = await _context.Blockers
                        .Where(b => b.BacklogItemId == backlogItem.Id && b.Status == BlockerStatus.Active)
                        .Select(b => new BlockerResponse
                        {
                            Id = b.Id,
                            Description = b.Description,
                            Severity = b.Severity.ToString(),
                            Status = b.Status.ToString(),
                            CreatedAt = b.CreatedAt
                        })
                        .ToListAsync();
                }

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
                    SubTasksCount = subTasks.Count,
                    CompletedSubTasksCount = subTasks.Count(st => st.Status == SubTaskStatus.Done),
                    CommentsCount = commentsCount,
                    AttachmentsCount = attachmentsCount,
                    ActiveBlockers = activeBlockers,
                    Efficiency = backlogItem.EstimatedHours.HasValue && backlogItem.ActualHours.HasValue && backlogItem.ActualHours.Value > 0
                        ? Math.Round((double)backlogItem.EstimatedHours.Value / backlogItem.ActualHours.Value * 100, 1)
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при маппинге BacklogItem {Id}", backlogItem.Id);
                throw;
            }
        }

        #endregion
    }
}