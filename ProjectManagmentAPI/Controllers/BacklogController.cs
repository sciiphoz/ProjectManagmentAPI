using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Extensions;
using ProjectManagementAPI.Interfaces;

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BacklogController : ControllerBase
    {
        private readonly IBacklogService _backlogService;
        private readonly ContextDb _context;

        public BacklogController(IBacklogService backlogService, ContextDb context)
        {
            _backlogService = backlogService;
            _context = context;
        }

        /// <summary>
        /// Проверяет, является ли текущий пользователь Viewer в проекте
        /// </summary>
        private async Task<bool> IsViewer(Guid projectId)
        {
            var userId = User.GetUserId();
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            return member?.RoleInProject == ProjectRole.Viewer;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<BacklogItemResponse>>> CreateBacklogItem(CreateBacklogItemRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<BacklogItemResponse>.Fail("Неверные данные"));

            if (await IsViewer(request.ProjectId))
                return Forbid();

            var response = await _backlogService.CreateBacklogItemAsync(request);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<BacklogItemResponse>>> GetBacklogItemById(Guid id)
        {
            var response = await _backlogService.GetBacklogItemByIdAsync(id);
            return Ok(response);
        }

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

        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<ApiResponse<PagedResult<BacklogItemResponse>>>> GetProjectBacklog(Guid projectId, [FromQuery] PagedRequest request)
        {
            var response = await _backlogService.GetProjectBacklogAsync(projectId, request);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<BacklogItemResponse>>> UpdateBacklogItem(Guid id, UpdateBacklogItemRequest request)
        {
            var item = await _context.BacklogItems.FindAsync(id);
            if (item != null && await IsViewer(item.ProjectId))
                return Forbid();

            var response = await _backlogService.UpdateBacklogItemAsync(id, request);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> DeleteBacklogItem(Guid id)
        {
            var item = await _context.BacklogItems.FindAsync(id);
            if (item != null && await IsViewer(item.ProjectId))
                return Forbid();

            var response = await _backlogService.DeleteBacklogItemAsync(id);
            return Ok(response);
        }

        [HttpPatch("{id}/status")]
        public async Task<ActionResult<ApiResponse<BacklogItemResponse>>> ChangeStatus(Guid id, ChangeTaskStatusRequest request)
        {
            var item = await _context.BacklogItems.FindAsync(id);
            if (item != null && await IsViewer(item.ProjectId))
                return Forbid();

            var response = await _backlogService.ChangeStatusAsync(id, request);
            return Ok(response);
        }

        [HttpPost("reorder")]
        public async Task<ActionResult<ApiResponse>> ReorderBacklog(ReorderBacklogRequest request)
        {
            var firstItem = await _context.BacklogItems.FindAsync(request.Items.FirstOrDefault()?.Id ?? Guid.Empty);
            if (firstItem != null && await IsViewer(firstItem.ProjectId))
                return Forbid();

            var response = await _backlogService.ReorderBacklogAsync(request);
            return Ok(response);
        }

        [HttpPost("{backlogItemId}/comments")]
        public async Task<ActionResult<ApiResponse<CommentResponse>>> AddComment(Guid backlogItemId, AddCommentRequest request)
        {
            var item = await _context.BacklogItems.FindAsync(backlogItemId);
            if (item != null && await IsViewer(item.ProjectId))
                return Forbid();

            var userId = User.GetUserId();
            var response = await _backlogService.AddCommentAsync(backlogItemId, request, userId);
            return Ok(response);
        }

        [HttpPut("comments/{commentId}")]
        public async Task<ActionResult<ApiResponse<CommentResponse>>> UpdateComment(Guid commentId, UpdateCommentRequest request)
        {
            var userId = User.GetUserId();
            var response = await _backlogService.UpdateCommentAsync(commentId, request, userId);
            return Ok(response);
        }

        [HttpDelete("comments/{commentId}")]
        public async Task<ActionResult<ApiResponse>> DeleteComment(Guid commentId)
        {
            var userId = User.GetUserId();
            var response = await _backlogService.DeleteCommentAsync(commentId, userId);
            return Ok(response);
        }

        [HttpPost("{backlogItemId}/attachments")]
        public async Task<ActionResult<ApiResponse<AttachmentResponse>>> UploadAttachment(Guid backlogItemId, [FromForm] UploadFileRequest request)
        {
            var item = await _context.BacklogItems.FindAsync(backlogItemId);
            if (item != null && await IsViewer(item.ProjectId))
                return Forbid();

            var currentUserId = User.GetUserId();
            var currentUser = await _context.Users.FindAsync(currentUserId);
            var currentUserName = currentUser?.FullName ?? "Неизвестный пользователь";

            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await request.File.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            var uploadRequest = new UploadAttachmentRequest
            {
                FileContent = fileBytes,
                FileName = request.File.FileName,
                MimeType = request.File.ContentType,
                UploadedById = currentUserId,
                UploadedByName = currentUserName
            };

            var response = await _backlogService.UploadAttachmentAsync(backlogItemId, uploadRequest);
            return Ok(response);
        }

        [HttpDelete("attachments/{attachmentId}")]
        public async Task<ActionResult<ApiResponse>> DeleteAttachment(Guid attachmentId)
        {
            var response = await _backlogService.DeleteAttachmentAsync(attachmentId);
            return Ok(response);
        }

        [HttpGet("attachments/{attachmentId}/download")]
        public async Task<IActionResult> DownloadAttachment(Guid attachmentId)
        {
            var fileData = await _backlogService.DownloadAttachmentAsync(attachmentId);
            if (fileData == null || fileData.Length == 0)
                return NotFound("Файл не найден");

            var attachment = await _context.Attachments.FindAsync(attachmentId);
            var fileName = attachment?.FileName ?? $"{attachmentId}.file";
            var mimeType = attachment?.MimeType ?? "application/octet-stream";

            return File(fileData, mimeType, fileName);
        }

        [HttpPost("{backlogItemId}/blockers")]
        public async Task<ActionResult<ApiResponse<BlockerResponse>>> AddBlocker(Guid backlogItemId, AddBlockerRequest request)
        {
            var item = await _context.BacklogItems.FindAsync(backlogItemId);
            if (item != null && await IsViewer(item.ProjectId))
                return Forbid();

            var response = await _backlogService.AddBlockerAsync(backlogItemId, request.Description, request.Severity);
            return Ok(response);
        }

        [HttpPatch("blockers/{blockerId}/resolve")]
        public async Task<ActionResult<ApiResponse>> ResolveBlocker(Guid blockerId, ResolveBlockerRequest request)
        {
            var response = await _backlogService.ResolveBlockerAsync(blockerId, request.ResolutionNote);
            return Ok(response);
        }
    }
}