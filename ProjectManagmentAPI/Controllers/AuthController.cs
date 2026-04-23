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
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(RegisterRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<AuthResponse>.Fail("Неверные данные",
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
                }

                var response = await _userService.RegisterAsync(request);
                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя");
                return StatusCode(500, ApiResponse<AuthResponse>.Fail("Внутренняя ошибка сервера"));
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse<AuthResponse>.Fail("Неверные данные"));
                }

                var response = await _userService.LoginAsync(request);
                return response.Success ? Ok(response) : Unauthorized(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя");
                return StatusCode(500, ApiResponse<AuthResponse>.Fail("Внутренняя ошибка сервера"));
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            try
            {
                var userId = User.GetUserId();
                var response = await _userService.LogoutAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе пользователя");
                return StatusCode(500, ApiResponse.Fail("Внутренняя ошибка сервера"));
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken(RefreshTokenRequest request)
        {
            try
            {
                var response = await _userService.RefreshTokenAsync(request.RefreshToken);
                return response.Success ? Ok(response) : Unauthorized(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении токена");
                return StatusCode(500, ApiResponse<AuthResponse>.Fail("Внутренняя ошибка сервера"));
            }
        }

        [HttpPost("confirm-email")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> ConfirmEmail(ConfirmEmailRequest request)
        {
            try
            {
                var response = await _userService.ConfirmEmailAsync(request.Token, request.Email);
                return response.Success ? Ok(response) : BadRequest(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при подтверждении email");
                return StatusCode(500, ApiResponse.Fail("Внутренняя ошибка сервера"));
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var response = await _userService.ForgotPasswordAsync(request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запросе сброса пароля");
                return StatusCode(500, ApiResponse.Fail("Внутренняя ошибка сервера"));
            }
        }

        [HttpPost("verify-reset-code")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> VerifyResetCode([FromBody] VerifyCodeRequest request)
        {
            try
            {
                var response = await _userService.VerifyResetCodeAsync(request.Email, request.Code);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки кода");
                return StatusCode(500, ApiResponse.Fail("Внутренняя ошибка сервера"));
            }
        }

        [HttpPost("reset-password-with-code")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse>> ResetPasswordWithCode([FromBody] ResetPasswordWithCodeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ApiResponse.Fail("Неверные данные"));
                }

                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    return BadRequest(ApiResponse.Fail("Пароли не совпадают"));
                }

                var response = await _userService.ResetPasswordWithCodeAsync(request.Email, request.Code, request.NewPassword);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка сброса пароля");
                return StatusCode(500, ApiResponse.Fail("Внутренняя ошибка сервера"));
            }
        }
    }
}