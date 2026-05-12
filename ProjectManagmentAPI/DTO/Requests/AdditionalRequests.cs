using System.ComponentModel.DataAnnotations;

namespace ProjectManagementAPI.DTO.Requests
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; }
    }

    public class AddBlockerRequest
    {
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Severity { get; set; } = "Medium";
    }

    public class ResolveBlockerRequest
    {
        [Required]
        [MaxLength(2000)]
        public string ResolutionNote { get; set; } = string.Empty;
    }

    public class AddRetrospectiveItemRequest
    {
        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}