// Interfaces/IRetrospectiveService.cs
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;

namespace ProjectManagementAPI.Interfaces
{
    public interface IRetrospectiveService
    {
        /// <summary>
        /// Получение доски ретроспективы для спринта
        /// </summary>
        Task<ApiResponse<RetrospectiveBoardResponse>> GetRetrospectiveBoardAsync(Guid sprintId, Guid currentUserId);

        /// <summary>
        /// Добавление элемента в ретроспективу
        /// </summary>
        Task<ApiResponse<RetrospectiveItemResponse>> AddRetrospectiveItemAsync(
            Guid sprintId,
            string category,
            string content,
            Guid userId);

        /// <summary>
        /// Голосование за элемент ретроспективы
        /// </summary>
        Task<ApiResponse> VoteRetrospectiveItemAsync(Guid itemId, Guid userId);

        /// <summary>
        /// Удаление голоса за элемент ретроспективы
        /// </summary>
        Task<ApiResponse> RemoveVoteAsync(Guid itemId, Guid userId);

        /// <summary>
        /// Удаление элемента ретроспективы
        /// </summary>
        Task<ApiResponse> DeleteRetrospectiveItemAsync(Guid itemId, Guid userId);
    }
}