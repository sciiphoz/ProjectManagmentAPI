// Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Extensions;
using ProjectManagementAPI.Interfaces;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<AuthResponse>.Fail("Неверные данные",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));

            var response = await _userService.RegisterAsync(request);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Вход в систему
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<AuthResponse>.Fail("Неверные данные"));

            var response = await _userService.LoginAsync(request);
            return response.Success ? Ok(response) : Unauthorized(response);
        }

        /// <summary>
        /// Выход из системы
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            var userId = User.GetUserId();
            var response = await _userService.LogoutAsync(userId);
            return Ok(response);
        }

        /// <summary>
        /// Обновление токена доступа
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(RefreshTokenRequest request)
        {
            var response = await _userService.RefreshTokenAsync(request.RefreshToken);
            return response.Success ? Ok(response) : Unauthorized(response);
        }

        /// <summary>
        /// Подтверждение email
        /// </summary>
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> ConfirmEmail(ConfirmEmailRequest request)
        {
            var response = await _userService.ConfirmEmailAsync(request.Token, request.Email);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        /// <summary>
        /// Запрос на сброс пароля
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> ForgotPassword(ForgotPasswordRequest request)
        {
            var response = await _userService.ForgotPasswordAsync(request.Email);
            return Ok(response);
        }

        /// <summary>
        /// Сброс пароля
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> ResetPassword(ResetPasswordRequest request)
        {
            var response = await _userService.ResetPasswordAsync(request);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}