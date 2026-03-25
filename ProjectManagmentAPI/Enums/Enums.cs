using System.ComponentModel;

namespace ProjectManagementAPI.Enums
{
    public enum GlobalRole
    {
        [Description("Пользователь")]
        User = 0,
        [Description("Администратор")]
        Admin = 1
    }

    public enum ProjectRole
    {
        [Description("Владелец продукта")]
        ProductOwner = 0,
        [Description("Scrum-мастер")]
        ScrumMaster = 1,
        [Description("Разработчик")]
        Developer = 2,
        [Description("Наблюдатель")]
        Viewer = 3
    }

    public enum SprintStatus
    {
        [Description("Запланирован")]
        Planned = 0,
        [Description("Активен")]
        Active = 1,
        [Description("Завершен")]
        Completed = 2,
        [Description("Отменен")]
        Cancelled = 3
    }

    public enum BacklogItemType
    {
        [Description("Пользовательская история")]
        UserStory = 0,
        [Description("Ошибка")]
        Bug = 1,
        [Description("Техническая задача")]
        TechnicalTask = 2,
        [Description("Улучшение")]
        Improvement = 3
    }

    public enum BacklogItemStatus
    {
        [Description("Бэклог")]
        Backlog = 0,
        [Description("К выполнению")]
        ToDo = 1,
        [Description("В работе")]
        InProgress = 2,
        [Description("На проверке")]
        Review = 3,
        [Description("Выполнено")]
        Done = 4
    }

    public enum SubTaskStatus
    {
        [Description("К выполнению")]
        ToDo = 0,
        [Description("В работе")]
        InProgress = 1,
        [Description("Выполнено")]
        Done = 2
    }

    public enum BlockerSeverity
    {
        [Description("Низкий")]
        Low = 0,
        [Description("Средний")]
        Medium = 1,
        [Description("Высокий")]
        High = 2,
        [Description("Критический")]
        Critical = 3
    }

    public enum BlockerStatus
    {
        [Description("Активен")]
        Active = 0,
        [Description("Разрешен")]
        Resolved = 1
    }

    public enum ActionType
    {
        [Description("Задача создана")]
        TaskCreated = 0,
        [Description("Задача обновлена")]
        TaskUpdated = 1,
        [Description("Статус задачи изменен")]
        TaskStatusChanged = 2,
        [Description("Задача удалена")]
        TaskDeleted = 3,
        [Description("Подзадача создана")]
        SubTaskCreated = 4,
        [Description("Подзадача обновлена")]
        SubTaskUpdated = 5,
        [Description("Статус подзадачи изменен")]
        SubTaskStatusChanged = 6,
        [Description("Спринт начат")]
        SprintStarted = 7,
        [Description("Спринт завершен")]
        SprintCompleted = 8,
        [Description("Спринт отменен")]
        SprintCancelled = 9,
        [Description("Комментарий добавлен")]
        CommentAdded = 10,
        [Description("Комментарий обновлен")]
        CommentUpdated = 11,
        [Description("Вложение добавлено")]
        AttachmentAdded = 12,
        [Description("Блокер создан")]
        BlockerCreated = 13,
        [Description("Блокер разрешен")]
        BlockerResolved = 14,
        [Description("Назначен исполнитель")]
        UserAssigned = 15,
        [Description("Исполнитель снят")]
        UserUnassigned = 16
    }
}