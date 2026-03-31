// DTO/Responses/UserResponses.cs
namespace ProjectManagementAPI.DTO.Responses
{
    /// <summary>
    /// Ответ после авторизации
    /// </summary>
    public class AuthResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpiresAt { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
    }

    /// <summary>
    /// Информация о пользователе
    /// </summary>
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public DateTime? EmailConfirmedAt { get; set; }
    }

    public class UserBriefResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}