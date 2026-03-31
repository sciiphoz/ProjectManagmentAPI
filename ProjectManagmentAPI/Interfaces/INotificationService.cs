// Interfaces/INotificationService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface INotificationService
    {
        /// <summary>
        /// Получение уведомлений пользователя
        /// </summary>
        Task<ApiResponse<PagedResult<NotificationResponse>>> GetUserNotificationsAsync(Guid userId, PagedRequest request);

        /// <summary>
        /// Получение количества непрочитанных уведомлений
        /// </summary>
        Task<ApiResponse<int>> GetUnreadCountAsync(Guid userId);

        /// <summary>
        /// Отметка уведомления как прочитанного
        /// </summary>
        Task<ApiResponse> MarkAsReadAsync(Guid notificationId, Guid userId);

        /// <summary>
        /// Отметка всех уведомлений как прочитанных
        /// </summary>
        Task<ApiResponse> MarkAllAsReadAsync(Guid userId);

        /// <summary>
        /// Создание уведомления
        /// </summary>
        Task CreateNotificationAsync(Guid userId, string title, string message, string type,
            string? actionUrl = null, Guid? relatedEntityId = null, string? relatedEntityType = null);

        /// <summary>
        /// Уведомление всех участников проекта
        /// </summary>
        Task NotifyProjectMembersAsync(Guid projectId, string title, string message, string type,
            string? actionUrl = null, Guid? relatedEntityId = null, string? relatedEntityType = null);

        /// <summary>
        /// Уведомление при упоминании
        /// </summary>
        Task NotifyMentionedUsersAsync(Guid backlogItemId, string commentContent, Guid commentAuthorId);
    }
}