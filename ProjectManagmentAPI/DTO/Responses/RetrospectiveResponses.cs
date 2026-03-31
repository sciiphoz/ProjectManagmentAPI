namespace ProjectManagementAPI.DTO.Responses
{
    public class RetrospectiveItemResponse
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int VoteCount { get; set; }
        public UserBriefResponse CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool HasUserVoted { get; set; }
    }

    public class RetrospectiveBoardResponse
    {
        public Guid SprintId { get; set; }
        public string SprintName { get; set; } = string.Empty;
        public List<RetrospectiveItemResponse> GoodItems { get; set; } = new();
        public List<RetrospectiveItemResponse> BadItems { get; set; } = new();
        public List<RetrospectiveItemResponse> Ideas { get; set; } = new();
        public List<RetrospectiveItemResponse> Actions { get; set; } = new();
    }
}