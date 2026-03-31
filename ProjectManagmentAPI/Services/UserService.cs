using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.Models;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.DTO.Requests;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProjectManagementAPI.Services
{
    public class UserService : IUserService
    {
        private readonly ContextDb _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(ContextDb context, IConfiguration configuration, IPasswordHasher passwordHasher)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }

        public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser != null)
            {
                return ApiResponse<AuthResponse>.Fail("Пользователь с таким логином или email уже существует");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                FullName = request.FullName,
                Role = GlobalRole.User,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmationToken = GenerateEmailConfirmationToken()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var authResponse = await GenerateAuthResponse(user);
            return ApiResponse<AuthResponse>.Ok(authResponse, "Регистрация успешна");
        }

        public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail);

            if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
            {
                return ApiResponse<AuthResponse>.Fail("Неверный логин или пароль");
            }

            if (!user.IsActive)
            {
                return ApiResponse<AuthResponse>.Fail("Учетная запись деактивирована");
            }

            var authResponse = await GenerateAuthResponse(user);
            return ApiResponse<AuthResponse>.Ok(authResponse, "Вход выполнен успешно");
        }

        public async Task<ApiResponse> LogoutAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
            }
            return ApiResponse.Ok("Выход выполнен успешно");
        }

        public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return ApiResponse<AuthResponse>.Fail("Недействительный или просроченный refresh token");
            }

            var authResponse = await GenerateAuthResponse(user);
            return ApiResponse<AuthResponse>.Ok(authResponse);
        }

        public async Task<ApiResponse> ConfirmEmailAsync(string token, string email)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.EmailConfirmationToken == token);

            if (user == null)
            {
                return ApiResponse.Fail("Недействительная ссылка подтверждения");
            }

            user.EmailConfirmedAt = DateTime.UtcNow;
            user.EmailConfirmationToken = null;
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Email подтвержден успешно");
        }

        public async Task<ApiResponse> ForgotPasswordAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return ApiResponse.Ok("Если пользователь существует, инструкции отправлены на email");
            }

            user.PasswordResetToken = GeneratePasswordResetToken();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Инструкции по сбросу пароля отправлены на email");
        }

        public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email &&
                                          u.PasswordResetToken == request.Token &&
                                          u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                return ApiResponse.Fail("Недействительная или просроченная ссылка сброса пароля");
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Пароль успешно изменен");
        }

        public async Task<ApiResponse<UserResponse>> GetUserByIdAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponse>.Fail("Пользователь не найден");
            }

            return ApiResponse<UserResponse>.Ok(MapToUserResponse(user));
        }

        public async Task<ApiResponse<UserResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ApiResponse<UserResponse>.Fail("Пользователь не найден");
            }

            if (!string.IsNullOrEmpty(request.FullName))
                user.FullName = request.FullName;

            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                var existingUser = await _context.Users
                    .AnyAsync(u => u.Email == request.Email && u.Id != userId);

                if (existingUser)
                {
                    return ApiResponse<UserResponse>.Fail("Email уже используется другим пользователем");
                }

                user.Email = request.Email;
                user.EmailConfirmedAt = null;
                user.EmailConfirmationToken = GenerateEmailConfirmationToken();
            }

            //user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<UserResponse>.Ok(MapToUserResponse(user), "Профиль обновлен");
        }

        public async Task<ApiResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return ApiResponse.Fail("Пользователь не найден");
            }

            if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.CurrentPassword))
            {
                return ApiResponse.Fail("Неверный текущий пароль");
            }

            user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return ApiResponse.Ok("Пароль успешно изменен");
        }

        public async Task<ApiResponse<PagedResult<UserResponse>>> GetAllUsersAsync(PagedRequest request)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(u => u.Username.Contains(request.SearchTerm) ||
                                          u.Email.Contains(request.SearchTerm) ||
                                          u.FullName.Contains(request.SearchTerm));
            }

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                query = request.SortDescending
                    ? query.OrderByDescending(u => EF.Property<object>(u, request.SortBy))
                    : query.OrderBy(u => EF.Property<object>(u, request.SortBy));
            }
            else
            {
                query = query.OrderBy(u => u.CreatedAt);
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => MapToUserResponse(u))
                .ToListAsync();

            var result = new PagedResult<UserResponse>
            {
                Items = users,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return ApiResponse<PagedResult<UserResponse>>.Ok(result);
        }

        public async Task<ApiResponse<List<UserBriefResponse>>> GetProjectUsersAsync(Guid projectId)
        {
            var users = await _context.ProjectMembers
                .Where(pm => pm.ProjectId == projectId)
                .Include(pm => pm.User)
                .Select(pm => new UserBriefResponse
                {
                    Id = pm.User.Id,
                    FullName = pm.User.FullName,
                    Username = pm.User.Username,
                    Email = pm.User.Email,
                    Role = pm.RoleInProject.ToString()
                })
                .ToListAsync();

            return ApiResponse<List<UserBriefResponse>>.Ok(users);
        }

        #region Private Methods

        private async Task<AuthResponse> GenerateAuthResponse(User user)
        {
            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Token = token,
                TokenExpiresAt = DateTime.UtcNow.AddHours(1),
                Role = user.Role.ToString()
            };
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "default-key-12345678901234567890"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(1);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private string GenerateEmailConfirmationToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        private string GeneratePasswordResetToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        private UserResponse MapToUserResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString(),
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                IsEmailConfirmed = user.EmailConfirmedAt.HasValue
            };
        }

        #endregion
    }

    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string hash, string password);
    }

    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string hash, string password)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}