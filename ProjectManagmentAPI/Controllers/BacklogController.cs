// Controllers/BacklogController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BacklogController : ControllerBase
    {
        private readonly IBacklogService _backlogService;

        public BacklogController(IBacklogService backlogService)
        {
            _backlogService = backlogService;
        }

        /// <summary>
        /// Создание элемента бэклога
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<BacklogItemResponse>>> CreateBacklogItem(CreateBacklogItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<BacklogItemResponse>.Fail("Неверные данные"));
            }

            var response = await _backlogService.CreateBacklogItemAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Получение элемента бэклога по ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<BacklogItemResponse>>> GetBacklogItemById(Guid id)
        {
            var response = await _backlogService.GetBacklogItemByIdAsync(id);
            return Ok(response);
        }

        /// <summary>
        /// Получение детальной информации о задаче (с подзадачами, комментариями)
        /// </summary>
        [HttpGet("{id}/detail")]
        public async Task<ActionResult<ApiResponse<BacklogItemDetailResponse>>> GetBacklogItemDetail(Guid id)
        {
            try
            {
                var response = await _backlogService.GetBacklogItemDetailAsync(id);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<BacklogItemDetailResponse>.Fail($"Внутренняя ошибка: {ex.Message}"));
            }
        }

        /// <summary>
        /// Получение бэклога проекта
        /// </summary>
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<ApiResponse<PagedResult<BacklogItemResponse>>>> GetProjectBacklog(Guid projectId, [FromQuery] PagedRequest request)
        {
            var response = await _backlogService.GetProjectBacklogAsync(projectId, request);
            return Ok(response);
        }

        /// <summary>
        /// Обновление элемента бэклога
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<BacklogItemResponse>>> UpdateBacklogItem(Guid id, UpdateBacklogItemRequest request)
        {
            var response = await _backlogService.UpdateBacklogItemAsync(id, request);
            return Ok(response);
        }

        /// <summary>
        /// Удаление элемента бэклога
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteBacklogItem(Guid id)
        {
            var response = await _backlogService.DeleteBacklogItemAsync(id);
            return Ok(response);
        }

        /// <summary>
        /// Изменение статуса задачи
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<ActionResult<ApiResponse<BacklogItemResponse>>> ChangeStatus(Guid id, ChangeTaskStatusRequest request)
        {
            var response = await _backlogService.ChangeStatusAsync(id, request);
            return Ok(response);
        }

        /// <summary>
        /// Переупорядочивание бэклога (drag-and-drop)
        /// </summary>
        [HttpPost("reorder")]
        public async Task<ActionResult<ApiResponse>> ReorderBacklog(ReorderBacklogRequest request)
        {
            var response = await _backlogService.ReorderBacklogAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Добавление комментария к задаче
        /// </summary>
        [HttpPost("{backlogItemId}/comments")]
        public async Task<ActionResult<ApiResponse<CommentResponse>>> AddComment(Guid backlogItemId, AddCommentRequest request)
        {
            var response = await _backlogService.AddCommentAsync(backlogItemId, request);
            return Ok(response);
        }

        /// <summary>
        /// Обновление комментария
        /// </summary>
        [HttpPut("comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<CommentResponse>>> UpdateComment(Guid commentId, UpdateCommentRequest request)
        {
            var response = await _backlogService.UpdateCommentAsync(commentId, request);
            return Ok(response);
        }

        /// <summary>
        /// Удаление комментария
        /// </summary>
        [HttpDelete("comments/{commentId}")]
        public async Task<ActionResult<ApiResponse>> DeleteComment(Guid commentId)
        {
            var response = await _backlogService.DeleteCommentAsync(commentId);
            return Ok(response);
        }

        /// <summary>
        /// Загрузка вложения
        /// </summary>
        [HttpPost("{backlogItemId}/attachments")]
        public async Task<ActionResult<ApiResponse<AttachmentResponse>>> UploadAttachment(Guid backlogItemId, [FromForm] UploadFileRequest request)
        {
            var uploadRequest = new UploadAttachmentRequest
            {
                FileContent = request.FileContent,
                FileName = request.File.FileName,
                MimeType = request.File.ContentType
            };
            var response = await _backlogService.UploadAttachmentAsync(backlogItemId, uploadRequest);
            return Ok(response);
        }

        /// <summary>
        /// Удаление вложения
        /// </summary>
        [HttpDelete("attachments/{attachmentId}")]
        public async Task<ActionResult<ApiResponse>> DeleteAttachment(Guid attachmentId)
        {
            var response = await _backlogService.DeleteAttachmentAsync(attachmentId);
            return Ok(response);
        }

        /// <summary>
        /// Скачивание вложения
        /// </summary>
        [HttpGet("attachments/{attachmentId}/download")]
        public async Task<IActionResult> DownloadAttachment(Guid attachmentId)
        {
            var fileData = await _backlogService.DownloadAttachmentAsync(attachmentId);
            if (fileData == null || fileData.Length == 0)
                return NotFound();

            return File(fileData, "application/octet-stream", $"{attachmentId}.file");
        }

        /// <summary>
        /// Добавление блокера
        /// </summary>
        [HttpPost("{backlogItemId}/blockers")]
        public async Task<ActionResult<ApiResponse<BlockerResponse>>> AddBlocker(Guid backlogItemId, AddBlockerRequest request)
        {
            var response = await _backlogService.AddBlockerAsync(backlogItemId, request.Description, request.Severity);
            return Ok(response);
        }

        /// <summary>
        /// Разрешение блокера
        /// </summary>
        [HttpPatch("blockers/{blockerId}/resolve")]
        public async Task<ActionResult<ApiResponse>> ResolveBlocker(Guid blockerId, ResolveBlockerRequest request)
        {
            var response = await _backlogService.ResolveBlockerAsync(blockerId, request.ResolutionNote);
            return Ok(response);
        }
    }
}