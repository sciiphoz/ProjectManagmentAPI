namespace ProjectManagementAPI.DTO.Requests
{
    public class GetActivityLogsRequest
    {
        public Guid ProjectId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public Guid? UserId { get; set; }
        public string? ActionType { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}