using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.Models;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.DataBaseContext;

namespace ProjectManagementAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ContextDb _context;

        public NotificationService(ContextDb context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResult<NotificationResponse>>> GetUserNotificationsAsync(Guid userId, PagedRequest request)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();

            var notifications = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    ActionUrl = n.ActionUrl,
                    RelatedEntityId = n.RelatedEntityId,
                    RelatedEntityType = n.RelatedEntityType,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync();

            var result = new PagedResult<NotificationResponse>
            {
                Items = notifications,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return ApiResponse<PagedResult<NotificationResponse>>.Ok(result);
        }

        public async Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId)
        {
            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return ApiResponse<int>.Ok(count);
        }

        public async Task<ApiResponse> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
            {
                return ApiResponse.Fail("Уведомление не найдено");
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Уведомление отмечено как прочитанное");
        }

        public async Task<ApiResponse> MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Все уведомления отмечены как прочитанные");
        }

        public async Task CreateNotificationAsync(Guid userId, string title, string message, string type,
            string? actionUrl = null, Guid? relatedEntityId = null, string? relatedEntityType = null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                ActionUrl = actionUrl,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task NotifyProjectMembersAsync(Guid projectId, string title, string message, string type,
            string? actionUrl = null, Guid? relatedEntityId = null, string? relatedEntityType = null)
        {
            var members = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId)
                .Select(pm => pm.UserId)
                .ToListAsync();

            foreach (var memberId in members)
            {
                await CreateNotificationAsync(memberId, title, message, type, actionUrl, relatedEntityId, relatedEntityType);
            }
        }

        public async Task NotifyMentionedUsersAsync(Guid backlogItemId, string commentContent, Guid commentAuthorId)
        {
            // Простой парсинг упоминаний @username
            var mentions = ExtractMentions(commentContent);

            if (!mentions.Any())
                return;

            var backlogItem = await _context.BacklogItems
                .Include(bi => bi.Project)
                .FirstOrDefaultAsync(bi => bi.Id == backlogItemId);

            if (backlogItem == null)
                return;

            var mentionedUsers = await _context.Users
                .Where(u => mentions.Contains(u.Username) && u.Id != commentAuthorId)
                .ToListAsync();

            foreach (var user in mentionedUsers)
            {
                await CreateNotificationAsync(
                    user.Id,
                    "Вас упомянули в комментарии",
                    $"{commentAuthorId} упомянул вас в комментарии к задаче '{backlogItem.Title}'",
                    "Info",
                    $"/backlog/{backlogItemId}",
                    backlogItemId,
                    "BacklogItem"
                );
            }
        }

        #region Private Methods

        private List<string> ExtractMentions(string content)
        {
            var mentions = new List<string>();
            var words = content.Split(' ', '\n', '\r', '\t');

            foreach (var word in words)
            {
                if (word.StartsWith("@") && word.Length > 1)
                {
                    mentions.Add(word.Substring(1));
                }
            }

            return mentions;
        }

        #endregion
    }
}