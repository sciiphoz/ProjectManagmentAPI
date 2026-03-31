// Controllers/NotificationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Extensions;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Получение уведомлений пользователя
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<NotificationResponse>>>> GetNotifications([FromQuery] PagedRequest request)
        {
            var userId = User.GetUserId();
            var response = await _notificationService.GetUserNotificationsAsync(userId, request);
            return Ok(response);
        }

        /// <summary>
        /// Получение количества непрочитанных уведомлений
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount()
        {
            var userId = User.GetUserId();
            var response = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(response);
        }

        /// <summary>
        /// Отметка уведомления как прочитанного
        /// </summary>
        [HttpPost("{notificationId}/read")]
        public async Task<ActionResult<ApiResponse>> MarkAsRead(Guid notificationId)
        {
            var userId = User.GetUserId();
            var response = await _notificationService.MarkAsReadAsync(notificationId, userId);
            return Ok(response);
        }

        /// <summary>
        /// Отметка всех уведомлений как прочитанных
        /// </summary>
        [HttpPost("mark-all-read")]
        public async Task<ActionResult<ApiResponse>> MarkAllAsRead()
        {
            var userId = User.GetUserId();
            var response = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(response);
        }
    }
}