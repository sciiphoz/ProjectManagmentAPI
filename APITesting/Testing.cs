using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Models;
using ProjectManagementAPI.Services;
using Xunit;

namespace ProjectManagementAPI.Tests.Services
{
    // ============================================================
    // “≈—“»–Œ¬¿Õ»≈ Ã≈“ŒƒŒÃ ´¡≈ÀŒ√Œ ﬂŸ» ¿ª
    // ============================================================

    public class ProjectServiceTests
    {
        private ContextDb CreateContext()
        {
            var options = new DbContextOptionsBuilder<ContextDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ContextDb(options);
        }

        [Fact]
        public async Task CreateProject_ValidData_CreatesProjectWithOwnerRole()
        {
            var context = CreateContext();
            var userId = Guid.NewGuid();
            context.Users.Add(new User { Id = userId, Username = "owner", Email = "o@mail.com", PasswordHash = "h", FullName = "Owner" });
            await context.SaveChangesAsync();

            var notificationMock = new Mock<INotificationService>();
            var emailMock = new Mock<IEmailService>();
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            var loggerMock = new Mock<ILogger<ProjectService>>();

            var service = new ProjectService(context, notificationMock.Object, emailMock.Object, configMock.Object, loggerMock.Object);

            var result = await service.CreateProjectAsync(new CreateProjectRequest { Name = "New Project" }, userId);

            Assert.True(result.Success);
            Assert.Equal("New Project", result.Data.Name);

            var member = await context.ProjectMembers.FirstOrDefaultAsync(pm => pm.ProjectId == result.Data.Id);
            Assert.NotNull(member);
            Assert.Equal(ProjectRole.ProductOwner, member.RoleInProject);
        }
    }

    // ============================================================
    public class SprintServiceTests
    {
        private ContextDb CreateContext()
        {
            var options = new DbContextOptionsBuilder<ContextDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ContextDb(options);
        }

        [Fact]
        public async Task CompleteSprint_ActiveSprint_ReturnsTasksToBacklogAndSavesVelocity()
        {
            var context = CreateContext();
            var projectId = Guid.NewGuid();
            var sprintId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            context.Projects.Add(new Project { Id = projectId, Name = "P", OwnerId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });
            context.Sprints.Add(new Sprint { Id = sprintId, ProjectId = projectId, Name = "Sprint 1", StartDate = DateTime.UtcNow.AddDays(-5), EndDate = DateTime.UtcNow.AddDays(5), IsActive = true, Status = SprintStatus.Active, CommittedStoryPoints = 8 });
            context.BacklogItems.Add(new BacklogItem { Id = taskId, ProjectId = projectId, SprintId = sprintId, Title = "Task 1", StoryPoints = 8, Status = BacklogItemStatus.Done });
            await context.SaveChangesAsync();

            var notificationMock = new Mock<INotificationService>();
            var loggerMock = new Mock<ILogger<SprintService>>();

            var service = new SprintService(context, notificationMock.Object, null, loggerMock.Object);

            var result = await service.CompleteSprintAsync(new CompleteSprintRequest { SprintId = sprintId, ReviewNotes = "Ok" });

            Assert.True(result.Success);
            var sprint = await context.Sprints.FindAsync(sprintId);
            Assert.Equal(SprintStatus.Completed, sprint.Status);
            Assert.False(sprint.IsActive);
            Assert.Equal(8, sprint.CompletedStoryPoints);

            var velocity = await context.SprintVelocities.FirstOrDefaultAsync(sv => sv.SprintId == sprintId);
            Assert.NotNull(velocity);
            Assert.Equal(8, velocity.Velocity);
        }
    }

    // ============================================================
    public class BacklogServiceTests
    {
        private ContextDb CreateContext()
        {
            var options = new DbContextOptionsBuilder<ContextDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ContextDb(options);
        }

        private Mock<Microsoft.AspNetCore.SignalR.IHubContext<ProjectManagementAPI.Hubs.CommentHub>> CreateHubMock()
        {
            var hubMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<ProjectManagementAPI.Hubs.CommentHub>>();
            var clientsMock = new Mock<Microsoft.AspNetCore.SignalR.IHubClients>();
            var clientProxyMock = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();

            clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);
            hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

            return hubMock;
        }

        [Fact]
        public async Task ChangeStatus_ToDone_AllSubtasksDone_Succeeds()
        {
            var context = CreateContext();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var subTaskId = Guid.NewGuid();

            context.Projects.Add(new Project { Id = projectId, Name = "P", OwnerId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });
            context.BacklogItems.Add(new BacklogItem { Id = taskId, ProjectId = projectId, Title = "Main Task", Status = BacklogItemStatus.Review });
            context.SubTasks.Add(new SubTask { Id = subTaskId, BacklogItemId = taskId, Title = "Sub", Status = SubTaskStatus.Done });
            await context.SaveChangesAsync();

            var notificationMock = new Mock<INotificationService>();
            var hubMock = CreateHubMock();
            var loggerMock = new Mock<ILogger<UserService>>();

            var service = new BacklogService(context, notificationMock.Object, hubMock.Object, loggerMock.Object);

            var result = await service.ChangeStatusAsync(taskId, new ChangeTaskStatusRequest { NewStatus = BacklogItemStatus.Done, UserId = Guid.NewGuid() });

            Assert.True(result.Success);
            var task = await context.BacklogItems.FindAsync(taskId);
            Assert.Equal(BacklogItemStatus.Done, task.Status);
            Assert.NotNull(task.CompletedAt);
        }

        [Fact]
        public async Task AddComment_AssigneeGetsNotification()
        {
            var context = CreateContext();
            var projectId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var commenterId = Guid.NewGuid();

            context.Projects.Add(new Project { Id = projectId, Name = "P", OwnerId = Guid.NewGuid(), CreatedAt = DateTime.UtcNow });
            context.BacklogItems.Add(new BacklogItem { Id = taskId, ProjectId = projectId, Title = "Task", AssigneeId = assigneeId, CreatedById = commenterId });
            context.Users.Add(new User { Id = assigneeId, Username = "dev", Email = "dev@m.com", PasswordHash = "h", FullName = "Dev" });
            context.Users.Add(new User { Id = commenterId, Username = "sm", Email = "sm@m.com", PasswordHash = "h", FullName = "SM" });
            await context.SaveChangesAsync();

            var notificationMock = new Mock<INotificationService>();
            var hubMock = CreateHubMock();
            var loggerMock = new Mock<ILogger<UserService>>();

            var service = new BacklogService(context, notificationMock.Object, hubMock.Object, loggerMock.Object);

            var result = await service.AddCommentAsync(taskId, new AddCommentRequest { Content = "Test comment" }, commenterId);

            Assert.True(result.Success, result.Message);

            notificationMock.Verify(n => n.CreateNotificationAsync(
                assigneeId,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                taskId,
                It.IsAny<string>()), Times.Once);
        }
    }

    // ============================================================
    public class NotificationServiceTests
    {
        private ContextDb CreateContext()
        {
            var options = new DbContextOptionsBuilder<ContextDb>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            return new ContextDb(options);
        }

        [Fact]
        public async Task MarkAsRead_ExistingNotification_SetsReadFlag()
        {
            var context = CreateContext();
            var userId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();

            context.Notifications.Add(new Notification
            {
                Id = notificationId,
                UserId = userId,
                Title = "Test",
                Message = "Test message",
                Type = "Info",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<NotificationService>>();
            var service = new NotificationService(context, loggerMock.Object);

            var result = await service.MarkAsReadAsync(notificationId, userId);

            Assert.True(result.Success);
            var notification = await context.Notifications.FindAsync(notificationId);
            Assert.True(notification.IsRead);
            Assert.NotNull(notification.ReadAt);
        }

        [Fact]
        public async Task GetUnreadCount_ReturnsCorrectNumber()
        {
            var context = CreateContext();
            var userId = Guid.NewGuid();

            context.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Title = "1", Message = "m", Type = "Info", IsRead = false, CreatedAt = DateTime.UtcNow });
            context.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Title = "2", Message = "m", Type = "Info", IsRead = false, CreatedAt = DateTime.UtcNow });
            context.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = userId, Title = "3", Message = "m", Type = "Info", IsRead = true, CreatedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var loggerMock = new Mock<ILogger<NotificationService>>();
            var service = new NotificationService(context, loggerMock.Object);

            var result = await service.GetUnreadCountAsync(userId);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data);
        }
    }
}