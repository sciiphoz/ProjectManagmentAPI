// DTO/Requests/UserRequests.cs
using System.ComponentModel.DataAnnotations;

namespace ProjectManagementAPI.DTO.Requests
{
    /// <summary>
    /// Запрос на регистрацию
    /// </summary>
    public class RegisterRequest
    {
        [Required]
        [MinLength(3, ErrorMessage = "Логин должен содержать минимум 3 символа")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Запрос на вход
    /// </summary>
    public class LoginRequest
    {
        [Required]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    /// <summary>
    /// Запрос на обновление профиля
    /// </summary>
    public class UpdateProfileRequest
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    public class InviteUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public Guid ProjectId { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;
    }
}