// DTO/Responses/ProjectResponses.cs
namespace ProjectManagementAPI.DTO.Responses
{
    public class ProjectResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public UserBriefResponse Owner { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsArchived { get; set; }

        public int MembersCount { get; set; }
        public int ActiveSprintsCount { get; set; }
        public int TotalTasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
    }

    public class ProjectBriefResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public string? OwnerName { get; set; }
    }

    public class ProjectMemberResponse
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public bool IsOwner { get; set; }
    }

    public class ProjectStatisticsResponse
    {
        public int TotalMembers { get; set; }
        public int TotalSprints { get; set; }
        public int ActiveSprints { get; set; }
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int TotalStoryPoints { get; set; }
        public int CompletedStoryPoints { get; set; }
        public double CompletionPercentage { get; set; }
    }
}