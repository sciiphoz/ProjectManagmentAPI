// DTO/Requests/SprintRequests.cs
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementAPI.DTO.Requests
{
    public class CreateSprintRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Goal { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public Guid ProjectId { get; set; }
    }

    public class UpdateSprintRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(2000)]
        public string? Goal { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
    }

    public class StartSprintRequest
    {
        [Required]
        public Guid SprintId { get; set; }

        [Required]
        public List<Guid> BacklogItemIds { get; set; } = new();
    }

    public class CompleteSprintRequest
    {
        [Required]
        public Guid SprintId { get; set; }

        public string? ReviewNotes { get; set; }
        public string? RetrospectiveNotes { get; set; }
    }

    public class MoveToSprintRequest
    {
        [Required]
        public List<Guid> BacklogItemIds { get; set; } = new();

        [Required]
        public Guid SprintId { get; set; }
    }
}