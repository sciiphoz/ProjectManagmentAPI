// Controllers/RetrospectiveController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.Extensions;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RetrospectiveController : ControllerBase
    {
        private readonly IRetrospectiveService _retrospectiveService;

        public RetrospectiveController(IRetrospectiveService retrospectiveService)
        {
            _retrospectiveService = retrospectiveService;
        }

        /// <summary>
        /// Получение доски ретроспективы для спринта
        /// </summary>
        [HttpGet("sprint/{sprintId}")]
        public async Task<ActionResult<ApiResponse<RetrospectiveBoardResponse>>> GetRetrospectiveBoard(Guid sprintId)
        {
            var userId = User.GetUserId();
            var response = await _retrospectiveService.GetRetrospectiveBoardAsync(sprintId, userId);
            return Ok(response);
        }

        /// <summary>
        /// Добавление элемента в ретроспективу
        /// </summary>
        [HttpPost("sprint/{sprintId}/items")]
        public async Task<ActionResult<ApiResponse<RetrospectiveItemResponse>>> AddRetrospectiveItem(
            Guid sprintId,
            [FromBody] AddRetrospectiveItemRequest request)
        {
            var userId = User.GetUserId();
            var response = await _retrospectiveService.AddRetrospectiveItemAsync(
                sprintId, request.Category, request.Content, userId);
            return Ok(response);
        }

        /// <summary>
        /// Голосование за элемент ретроспективы
        /// </summary>
        [HttpPost("items/{itemId}/vote")]
        public async Task<ActionResult<ApiResponse>> VoteRetrospectiveItem(Guid itemId)
        {
            var userId = User.GetUserId();
            var response = await _retrospectiveService.VoteRetrospectiveItemAsync(itemId, userId);
            return Ok(response);
        }

        /// <summary>
        /// Удаление голоса за элемент ретроспективы
        /// </summary>
        [HttpDelete("items/{itemId}/vote")]
        public async Task<ActionResult<ApiResponse>> RemoveVote(Guid itemId)
        {
            var userId = User.GetUserId();
            var response = await _retrospectiveService.RemoveVoteAsync(itemId, userId);
            return Ok(response);
        }

        /// <summary>
        /// Удаление элемента ретроспективы
        /// </summary>
        [HttpDelete("items/{itemId}")]
        public async Task<ActionResult<ApiResponse>> DeleteRetrospectiveItem(Guid itemId)
        {
            var userId = User.GetUserId();
            var response = await _retrospectiveService.DeleteRetrospectiveItemAsync(itemId, userId);
            return Ok(response);
        }
    }
}