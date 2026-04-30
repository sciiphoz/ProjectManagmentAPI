// Interfaces/IBacklogService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface IBacklogService
    {
        // Управление элементами бэклога
        Task<ApiResponse<BacklogItemResponse>> CreateBacklogItemAsync(CreateBacklogItemRequest request);
        Task<ApiResponse<BacklogItemResponse>> GetBacklogItemByIdAsync(Guid id);
        Task<ApiResponse<PagedResult<BacklogItemResponse>>> GetProjectBacklogAsync(Guid projectId, PagedRequest request);
        Task<ApiResponse<BacklogItemResponse>> UpdateBacklogItemAsync(Guid id, UpdateBacklogItemRequest request);
        Task<ApiResponse> DeleteBacklogItemAsync(Guid id);

        // Управление статусом
        Task<ApiResponse<BacklogItemResponse>> ChangeStatusAsync(Guid id, ChangeTaskStatusRequest request);

        // Управление порядком (drag-and-drop)
        Task<ApiResponse> ReorderBacklogAsync(ReorderBacklogRequest request);

        // Комментарии
        Task<ApiResponse<CommentResponse>> AddCommentAsync(Guid backlogItemId, AddCommentRequest request, Guid userId);
        Task<ApiResponse<CommentResponse>> UpdateCommentAsync(Guid commentId, UpdateCommentRequest request, Guid userId);
        Task<ApiResponse> DeleteCommentAsync(Guid commentId, Guid userId);

        // Вложения
        Task<ApiResponse<AttachmentResponse>> UploadAttachmentAsync(Guid backlogItemId, UploadAttachmentRequest request);
        Task<ApiResponse> DeleteAttachmentAsync(Guid attachmentId);
        Task<byte[]> DownloadAttachmentAsync(Guid attachmentId);

        // Блокеры
        Task<ApiResponse<BlockerResponse>> AddBlockerAsync(Guid backlogItemId, string description, string severity);
        Task<ApiResponse> ResolveBlockerAsync(Guid blockerId, string resolutionNote);

        // Детальная информация
        Task<ApiResponse<BacklogItemDetailResponse>> GetBacklogItemDetailAsync(Guid id);
    }
}