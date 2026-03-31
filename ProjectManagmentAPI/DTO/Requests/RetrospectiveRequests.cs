using System.ComponentModel.DataAnnotations;

namespace ProjectManagementAPI.DTO.Requests
{
    public class RetrospectiveRequests
    {
        public class AddRetrospectiveItemRequest
        {
            [Required]
            [MaxLength(50)]
            public string Category { get; set; } = string.Empty;

            [Required]
            [MaxLength(2000)]
            public string Content { get; set; } = string.Empty;
        }

        public class VoteRetrospectiveItemRequest
        {
            [Required]
            public Guid ItemId { get; set; }

            [Required]
            public bool AddVote { get; set; } = true;
        }
    }
}
