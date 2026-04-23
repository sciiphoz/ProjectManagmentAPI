// Interfaces/IUserService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface IUserService
    {
        // Аутентификация
        Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);
        Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);
        Task<ApiResponse> LogoutAsync(Guid userId);
        Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponse> ConfirmEmailAsync(string token, string email);
        Task<ApiResponse> ForgotPasswordAsync(string email);
        Task<ApiResponse> VerifyResetCodeAsync(string email, string code);
        Task<ApiResponse> ResetPasswordWithCodeAsync(string email, string code, string newPassword);

        // Управление пользователями
        Task<ApiResponse<UserResponse>> GetUserByIdAsync(Guid userId);
        Task<ApiResponse<UserResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        Task<ApiResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<ApiResponse<PagedResult<UserResponse>>> GetAllUsersAsync(PagedRequest request);
        Task<ApiResponse<List<UserBriefResponse>>> GetProjectUsersAsync(Guid projectId);
    }
}