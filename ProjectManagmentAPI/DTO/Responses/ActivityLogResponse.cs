// DTOs/Responses/ActivityLogResponse.cs
namespace ProjectManagementAPI.DTO.Responses
{
    public class ActivityLogResponse
    {
        public long Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public UserBriefResponse User { get; set; } = null!;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}