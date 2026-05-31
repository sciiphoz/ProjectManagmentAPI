using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Hubs;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Models;
using System;

namespace ProjectManagementAPI.Services
{
    public class RetrospectiveService : BaseService, IRetrospectiveService
    {
        private readonly ContextDb _context;
        private readonly IHubContext<CommentHub> _hubContext;

        public RetrospectiveService(ContextDb context, IHubContext<CommentHub> hubContext, ILogger<RetrospectiveService> logger) : base(logger)
        {
            _context = context;
            _hubContext = hubContext;
        }
        public async Task<ApiResponse<RetrospectiveBoardResponse>> GetRetrospectiveBoardAsync(Guid sprintId, Guid currentUserId)
        {
            try
            {
                var sprint = await _context.Sprints.FindAsync(sprintId);
                if (sprint == null)
                {
                    return ApiResponse<RetrospectiveBoardResponse>.Fail("Спринт не найден");
                }

                var items = await _context.RetrospectiveItems
                    .Where(ri => ri.SprintId == sprintId)
                    .Include(ri => ri.CreatedBy)
                    .Include(ri => ri.Votes)
                    .ToListAsync();

                var userVotesList = await _context.RetrospectiveVotes
                    .Where(rv => rv.UserId == currentUserId)
                    .Select(rv => rv.RetrospectiveItemId)
                    .ToListAsync();

                var userVotes = userVotesList.ToHashSet();

                var response = new RetrospectiveBoardResponse
                {
                    SprintId = sprint.Id,
                    SprintName = sprint.Name,
                    GoodItems = items
                        .Where(i => i.Category == "Good")
                        .Select(i => MapToResponse(i, userVotes.Contains(i.Id)))
                        .ToList(),
                    BadItems = items
                        .Where(i => i.Category == "Bad")
                        .Select(i => MapToResponse(i, userVotes.Contains(i.Id)))
                        .ToList(),
                    Ideas = items
                        .Where(i => i.Category == "Idea")
                        .Select(i => MapToResponse(i, userVotes.Contains(i.Id)))
                        .ToList(),
                    Actions = items
                        .Where(i => i.Category == "Action")
                        .Select(i => MapToResponse(i, userVotes.Contains(i.Id)))
                        .ToList()
                };

                return ApiResponse<RetrospectiveBoardResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения доски ретроспективы спринта {SprintId}", sprintId);
                return ApiResponse<RetrospectiveBoardResponse>.Fail("Произошла ошибка при загрузке ретроспективы");
            }
        }

        public async Task<ApiResponse<RetrospectiveItemResponse>> AddRetrospectiveItemAsync(
            Guid sprintId, string category, string content, Guid userId)
        {
            try
            {
                var sprint = await _context.Sprints.FindAsync(sprintId);
                if (sprint == null)
                {
                    return ApiResponse<RetrospectiveItemResponse>.Fail("Спринт не найден");
                }

                var validCategories = new[] { "Good", "Bad", "Idea", "Action" };
                if (!validCategories.Contains(category))
                {
                    return ApiResponse<RetrospectiveItemResponse>.Fail("Неверная категория");
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ApiResponse<RetrospectiveItemResponse>.Fail("Пользователь не найден");
                }

                var item = new RetrospectiveItem
                {
                    Id = Guid.NewGuid(),
                    SprintId = sprintId,
                    CreatedById = userId,
                    Category = category,
                    Content = content,
                    CreatedAt = DateTime.UtcNow
                };

                _context.RetrospectiveItems.Add(item);

                var response = new RetrospectiveItemResponse
                {
                    Id = item.Id,
                    Category = item.Category,
                    Content = item.Content,
                    VoteCount = 0,
                    CreatedBy = new UserBriefResponse
                    {
                        Id = user.Id,
                        FullName = user.FullName,
                        Username = user.Username
                    },
                    CreatedAt = item.CreatedAt,
                    HasUserVoted = false
                };

                await _context.SaveChangesAsync();
                await _hubContext.Clients.Group($"retro-{sprintId}").SendAsync("RetroItemAdded", response);
                return ApiResponse<RetrospectiveItemResponse>.Ok(response, "Элемент добавлен");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления элемента ретроспективы");
                return ApiResponse<RetrospectiveItemResponse>.Fail("Произошла ошибка при добавлении элемента");
            }
        }

        public async Task<ApiResponse> VoteRetrospectiveItemAsync(Guid itemId, Guid userId)
        {
            try
            {
                var item = await _context.RetrospectiveItems
                    .Include(i => i.CreatedBy)
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (item == null)
                {
                    return ApiResponse.Fail("Элемент не найден");
                }

                var existingVote = await _context.RetrospectiveVotes
                    .FirstOrDefaultAsync(rv => rv.RetrospectiveItemId == itemId && rv.UserId == userId);

                if (existingVote != null)
                {
                    return ApiResponse.Fail("Вы уже голосовали за этот элемент");
                }

                var vote = new RetrospectiveVote
                {
                    Id = Guid.NewGuid(),
                    RetrospectiveItemId = itemId,
                    UserId = userId,
                    VotedAt = DateTime.UtcNow
                };

                _context.RetrospectiveVotes.Add(vote);
                item.VoteCount++;
                await _context.SaveChangesAsync();

                var updatedItem = new RetrospectiveItemResponse
                {
                    Id = item.Id,
                    Category = item.Category,
                    Content = item.Content,
                    VoteCount = item.VoteCount,
                    CreatedBy = new UserBriefResponse
                    {
                        Id = item.CreatedBy?.Id ?? item.CreatedById,
                        FullName = item.CreatedBy?.FullName ?? "Участник",
                        Username = item.CreatedBy?.Username ?? "user"
                    },
                    CreatedAt = item.CreatedAt,
                    HasUserVoted = false
                };

                await _hubContext.Clients.Group($"retro-{item.SprintId}")
                    .SendAsync("RetroVoteUpdated", updatedItem);
                return ApiResponse.Ok("Голос добавлен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка голосования за элемент ретроспективы {ItemId}", itemId);
                return ApiResponse.Fail("Произошла ошибка при голосовании");
            }
        }

        public async Task<ApiResponse> RemoveVoteAsync(Guid itemId, Guid userId)
        {
            try
            {
                var vote = await _context.RetrospectiveVotes
                    .FirstOrDefaultAsync(rv => rv.RetrospectiveItemId == itemId && rv.UserId == userId);

                if (vote == null)
                {
                    return ApiResponse.Fail("Голос не найден");
                }
                
                var item = await _context.RetrospectiveItems
                    .Include(i => i.CreatedBy)
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (item != null)
                {
                    item.VoteCount = Math.Max(0, item.VoteCount - 1);
                }

                _context.RetrospectiveVotes.Remove(vote);
                await _context.SaveChangesAsync();

                var updatedItem = new RetrospectiveItemResponse
                {
                    Id = item.Id,
                    Category = item.Category,
                    Content = item.Content,
                    VoteCount = item.VoteCount,
                    CreatedBy = new UserBriefResponse
                    {
                        Id = item.CreatedBy?.Id ?? item.CreatedById,
                        FullName = item.CreatedBy?.FullName ?? "Участник",
                        Username = item.CreatedBy?.Username ?? "user"
                    },
                    CreatedAt = item.CreatedAt,
                    HasUserVoted = false
                };

                await _hubContext.Clients.Group($"retro-{item.SprintId}")
                    .SendAsync("RetroVoteUpdated", updatedItem);
                return ApiResponse.Ok("Голос удален");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления голоса за элемент ретроспективы {ItemId}", itemId);
                return ApiResponse.Fail("Произошла ошибка при удалении голоса");
            }
        }

        public async Task<ApiResponse> DeleteRetrospectiveItemAsync(Guid itemId, Guid userId)
        {
            try
            {
                var item = await _context.RetrospectiveItems
                    .Include(i => i.Votes)
                    .FirstOrDefaultAsync(i => i.Id == itemId);

                if (item == null)
                {
                    return ApiResponse.Fail("Элемент не найден");
                }

                var sprintId = item.SprintId; 

                var isCreator = item.CreatedById == userId;

                if (!isCreator)
                {
                    var sprint = await _context.Sprints.FindAsync(item.SprintId);
                    if (sprint != null)
                    {
                        var isScrumMaster = await _context.ProjectMembers
                            .AnyAsync(pm => pm.ProjectId == sprint.ProjectId &&
                                            pm.UserId == userId &&
                                            pm.RoleInProject == ProjectRole.ScrumMaster);

                        if (!isScrumMaster)
                        {
                            return ApiResponse.Fail("У вас нет прав на удаление этого элемента");
                        }
                    }
                }

                _context.RetrospectiveItems.Remove(item);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group($"retro-{sprintId}").SendAsync("RetroItemDeleted", itemId);
                return ApiResponse.Ok("Элемент удален");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления элемента ретроспективы {ItemId}", itemId);
                return ApiResponse.Fail("Произошла ошибка при удалении элемента");
            }
        }

        #region Private Methods

        private RetrospectiveItemResponse MapToResponse(RetrospectiveItem item, bool hasUserVoted)
        {
            return new RetrospectiveItemResponse
            {
                Id = item.Id,
                Category = item.Category,
                Content = item.Content,
                VoteCount = item.VoteCount,
                CreatedBy = item.CreatedBy != null ? new UserBriefResponse
                {
                    Id = item.CreatedBy.Id,
                    FullName = item.CreatedBy.FullName,
                    Username = item.CreatedBy.Username
                } : new UserBriefResponse
                {
                    Id = item.CreatedById,
                    FullName = "Участник",
                    Username = "user"
                },
                CreatedAt = item.CreatedAt,
                HasUserVoted = hasUserVoted
            };
        }

        #endregion
    }
}