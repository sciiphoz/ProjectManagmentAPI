// Controllers/UsersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Extensions;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Получение информации о текущем пользователе
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> GetCurrentUser()
        {
            var userId = User.GetUserId();
            var response = await _userService.GetUserByIdAsync(userId);
            return Ok(response);
        }

        /// <summary>
        /// Получение информации о пользователе по ID
        /// </summary>
        [HttpGet("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> GetUserById(Guid userId)
        {
            var response = await _userService.GetUserByIdAsync(userId);
            return Ok(response);
        }

        /// <summary>
        /// Обновление профиля текущего пользователя
        /// </summary>
        [HttpPut("me")]
        public async Task<ActionResult<ApiResponse<UserResponse>>> UpdateProfile(UpdateProfileRequest request)
        {
            var userId = User.GetUserId();
            var response = await _userService.UpdateProfileAsync(userId, request);
            return Ok(response);
        }

        /// <summary>
        /// Смена пароля
        /// </summary>
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponse>> ChangePassword(ChangePasswordRequest request)
        {
            var userId = User.GetUserId();
            var response = await _userService.ChangePasswordAsync(userId, request);
            return Ok(response);
        }

        /// <summary>
        /// Получение всех пользователей (для админа)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<PagedResult<UserResponse>>>> GetAllUsers([FromQuery] PagedRequest request)
        {
            var response = await _userService.GetAllUsersAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Получение всех пользователей проекта
        /// </summary>
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<ApiResponse<List<UserBriefResponse>>>> GetProjectUsers(Guid projectId)
        {
            var response = await _userService.GetProjectUsersAsync(projectId);
            return Ok(response);
        }
    }
}