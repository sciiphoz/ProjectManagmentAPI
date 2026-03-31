using System.ComponentModel.DataAnnotations;

namespace ProjectManagementAPI.DTO.Requests
{
    public class AuthRequests
    {
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

        public class LoginRequest
        {
            [Required]
            public string UsernameOrEmail { get; set; } = string.Empty;

            [Required]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; } = false;
        }

        public class RefreshTokenRequest
        {
            [Required]
            public string RefreshToken { get; set; } = string.Empty;
        }

        public class ConfirmEmailRequest
        {
            [Required]
            public string Token { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public class ForgotPasswordRequest
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public class ResetPasswordRequest
        {
            [Required]
            public string Token { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [MinLength(6)]
            public string NewPassword { get; set; } = string.Empty;

            [Required]
            [Compare("NewPassword")]
            public string ConfirmNewPassword { get; set; } = string.Empty;
        }
    }
}
