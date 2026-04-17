// DTO/Requests/ProjectRequests.cs
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementAPI.DTO.Requests
{
    public class CreateProjectRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }
    }

    public class UpdateProjectRequest
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        public bool? IsArchived { get; set; }
    }

    public class AddProjectMemberRequest
    {
        public Guid? UserId { get; set; }  

        [EmailAddress]
        public string? Email { get; set; } 

        [Required]
        public string Role { get; set; } = string.Empty;
    }
    public class UpdateMemberRoleRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string NewRole { get; set; } = string.Empty;
    }

    public class RemoveMemberRequest
    {
        [Required]
        public Guid UserId { get; set; }
    }
}