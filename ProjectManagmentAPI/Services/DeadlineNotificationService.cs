using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Interfaces;

namespace ProjectManagementAPI.Services
{
    public class DeadlineNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DeadlineNotificationService> _logger;

        public DeadlineNotificationService(IServiceProvider serviceProvider, ILogger<DeadlineNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckDeadlines();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка проверки дедлайнов");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task CheckDeadlines()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ContextDb>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var dueTomorrowTasks = await context.BacklogItems
                .Include(bi => bi.Sprint)
                .Include(bi => bi.Assignee)
                .Where(bi => bi.Sprint != null
                    && bi.Sprint.EndDate.Date == tomorrow
                    && bi.Status != BacklogItemStatus.Done
                    && bi.AssigneeId != null)
                .ToListAsync();

            foreach (var task in dueTomorrowTasks)
            {
                var alreadyNotified = await context.Notifications.AnyAsync(n =>
                    n.RelatedEntityId == task.Id
                    && n.RelatedEntityType == "BacklogItem"
                    && n.Title == "Дедлайн завтра"
                    && n.CreatedAt.Date == today);

                if (!alreadyNotified)
                {
                    await notificationService.CreateNotificationAsync(
                        task.AssigneeId!.Value,
                        "Дедлайн завтра",
                        $"Задача «{task.Title}» должна быть завершена завтра ({tomorrow:dd.MM.yyyy}).",
                        "Warning",
                        $"/sprints/{task.SprintId}/{task.Id}",
                        task.Id,
                        "BacklogItem"
                    );
                }
            }

            var overdueTasks = await context.BacklogItems
                .Include(bi => bi.Sprint)
                .Include(bi => bi.Assignee)
                .Where(bi => bi.Sprint != null
                    && bi.Sprint.EndDate.Date < today
                    && bi.Status != BacklogItemStatus.Done
                    && bi.AssigneeId != null)
                .ToListAsync();

            foreach (var task in overdueTasks)
            {
                var alreadyNotified = await context.Notifications.AnyAsync(n =>
                    n.RelatedEntityId == task.Id
                    && n.RelatedEntityType == "BacklogItem"
                    && n.Title == "Задача просрочена"
                    && n.CreatedAt.Date == today);

                if (!alreadyNotified)
                {
                    var daysOverdue = (today - task.Sprint!.EndDate.Date).Days;
                    await notificationService.CreateNotificationAsync(
                        task.AssigneeId!.Value,
                        "Задача просрочена",
                        $"Задача «{task.Title}» просрочена на {daysOverdue} дн. (дедлайн был {task.Sprint.EndDate:dd.MM.yyyy}).",
                        "Error",
                        $"/sprints/{task.SprintId}/{task.Id}",
                        task.Id,
                        "BacklogItem"
                    );
                }
            }
        }
    }
}