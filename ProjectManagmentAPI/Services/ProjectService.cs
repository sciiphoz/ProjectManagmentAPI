using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Enums;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Models;
using System;

namespace ProjectManagementAPI.Services
{
    public class ProjectService : BaseService, IProjectService
    {
        private readonly ContextDb _context;
        private readonly INotificationService _notificationService;

        public ProjectService(
            ContextDb context,
            INotificationService notificationService,
            ILogger<ProjectService> logger) : base(logger)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ApiResponse<ProjectResponse>> CreateProjectAsync(CreateProjectRequest request, Guid ownerId)
        {
            try
            {
                var project = new Project
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    OwnerId = ownerId,
                    CreatedAt = DateTime.UtcNow,
                    IsArchived = false
                };

                _context.Projects.Add(project);

                var projectMember = new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = ownerId,
                    RoleInProject = ProjectRole.ProductOwner,
                    JoinedAt = DateTime.UtcNow
                };

                _context.ProjectMembers.Add(projectMember);
                await _context.SaveChangesAsync();

                var response = await MapToProjectResponse(project);
                return ApiResponse<ProjectResponse>.Ok(response, "Проект успешно создан");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания проекта");
                return ApiResponse<ProjectResponse>.Fail("Произошла ошибка при создании проекта");
            }
        }

        public async Task<ApiResponse<ProjectResponse>> GetProjectByIdAsync(Guid projectId)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.Owner)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                {
                    return ApiResponse<ProjectResponse>.Fail("Проект не найден");
                }

                var response = await MapToProjectResponse(project);
                return ApiResponse<ProjectResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения проекта {ProjectId}", projectId);
                return ApiResponse<ProjectResponse>.Fail("Произошла ошибка при получении данных проекта");
            }
        }

        public async Task<ApiResponse<PagedResult<ProjectResponse>>> GetUserProjectsAsync(Guid userId, PagedRequest request)
        {
            try
            {
                var query = _context.ProjectMembers
                    .Where(pm => pm.UserId == userId)
                    .Include(pm => pm.Project)
                        .ThenInclude(p => p.Owner)
                    .Select(pm => pm.Project)
                    .Where(p => !p.IsArchived);

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = query.Where(p => p.Name.Contains(request.SearchTerm) ||
                                              (p.Description != null && p.Description.Contains(request.SearchTerm)));
                }

                var totalCount = await query.CountAsync();

                var projects = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var projectResponses = new List<ProjectResponse>();
                foreach (var project in projects)
                {
                    projectResponses.Add(await MapToProjectResponse(project));
                }

                var result = new PagedResult<ProjectResponse>
                {
                    Items = projectResponses,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };

                return ApiResponse<PagedResult<ProjectResponse>>.Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения проектов пользователя {UserId}", userId);
                return ApiResponse<PagedResult<ProjectResponse>>.Fail("Произошла ошибка при получении списка проектов");
            }
        }

        public async Task<ApiResponse<ProjectResponse>> UpdateProjectAsync(Guid projectId, UpdateProjectRequest request)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    return ApiResponse<ProjectResponse>.Fail("Проект не найден");
                }

                if (request.Name != null)
                    project.Name = request.Name;

                if (request.Description != null)
                    project.Description = request.Description;

                if (request.IsArchived.HasValue)
                    project.IsArchived = request.IsArchived.Value;

                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var response = await MapToProjectResponse(project);
                return ApiResponse<ProjectResponse>.Ok(response, "Проект обновлен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления проекта {ProjectId}", projectId);
                return ApiResponse<ProjectResponse>.Fail("Произошла ошибка при обновлении проекта");
            }
        }

        public async Task<ApiResponse> DeleteProjectAsync(Guid projectId)
        {
            try
            {
                var project = await _context.Projects
                    .Include(p => p.Sprints)
                    .Include(p => p.BacklogItems)
                    .FirstOrDefaultAsync(p => p.Id == projectId);

                if (project == null)
                {
                    return ApiResponse.Fail("Проект не найден");
                }

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();

                return ApiResponse.Ok("Проект удален");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления проекта {ProjectId}", projectId);
                return ApiResponse.Fail("Произошла ошибка при удалении проекта");
            }
        }

        public async Task<ApiResponse> ArchiveProjectAsync(Guid projectId)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    return ApiResponse.Fail("Проект не найден");
                }

                project.IsArchived = true;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ApiResponse.Ok("Проект архивирован");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка архивации проекта {ProjectId}", projectId);
                return ApiResponse.Fail("Произошла ошибка при архивации проекта");
            }
        }

        public async Task<ApiResponse> RestoreProjectAsync(Guid projectId)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    return ApiResponse.Fail("Проект не найден");
                }

                project.IsArchived = false;
                project.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ApiResponse.Ok("Проект восстановлен из архива");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка восстановления проекта {ProjectId}", projectId);
                return ApiResponse.Fail("Произошла ошибка при восстановлении проекта");
            }
        }

        public async Task<ApiResponse<List<ProjectMemberResponse>>> GetProjectMembersAsync(Guid projectId)
        {
            try
            {
                var members = await _context.ProjectMembers
                    .Where(pm => pm.ProjectId == projectId)
                    .Include(pm => pm.User)
                    .ToListAsync();

                var project = await _context.Projects.FindAsync(projectId);

                var responses = members.Select(m => new ProjectMemberResponse
                {
                    UserId = m.User.Id,
                    FullName = m.User.FullName,
                    Username = m.User.Username,
                    Email = m.User.Email,
                    Role = m.RoleInProject.ToString(),
                    JoinedAt = m.JoinedAt,
                    IsOwner = m.User.Id == project?.OwnerId
                }).ToList();

                return ApiResponse<List<ProjectMemberResponse>>.Ok(responses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения участников проекта {ProjectId}", projectId);
                return ApiResponse<List<ProjectMemberResponse>>.Fail("Произошла ошибка при получении участников");
            }
        }

        public async Task<ApiResponse<ProjectMemberResponse>> AddMemberAsync(Guid projectId, AddProjectMemberRequest request)
        {
            try
            {
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    return ApiResponse<ProjectMemberResponse>.Fail("Проект не найден");
                }

                var user = await _context.Users.FindAsync(request.UserId);
                if (user == null)
                {
                    return ApiResponse<ProjectMemberResponse>.Fail("Пользователь не найден");
                }

                var existingMember = await _context.ProjectMembers
                    .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == request.UserId);

                if (existingMember)
                {
                    return ApiResponse<ProjectMemberResponse>.Fail("Пользователь уже является участником проекта");
                }

                if (!Enum.TryParse<ProjectRole>(request.Role, true, out var role))
                {
                    return ApiResponse<ProjectMemberResponse>.Fail("Неверная роль");
                }

                var member = new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = request.UserId,
                    RoleInProject = role,
                    JoinedAt = DateTime.UtcNow
                };

                _context.ProjectMembers.Add(member);
                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    "Приглашение в проект",
                    $"Вы были добавлены в проект '{project.Name}' в роли {role}",
                    "Info",
                    $"/projects/{projectId}",
                    projectId,
                    "Project"
                );

                var response = new ProjectMemberResponse
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    Role = role.ToString(),
                    JoinedAt = member.JoinedAt,
                    IsOwner = false
                };

                return ApiResponse<ProjectMemberResponse>.Ok(response, "Участник добавлен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка добавления участника в проект {ProjectId}", projectId);
                return ApiResponse<ProjectMemberResponse>.Fail("Произошла ошибка при добавлении участника");
            }
        }

        public async Task<ApiResponse> UpdateMemberRoleAsync(Guid projectId, UpdateMemberRoleRequest request)
        {
            try
            {
                var member = await _context.ProjectMembers
                    .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == request.UserId);

                if (member == null)
                {
                    return ApiResponse.Fail("Участник не найден");
                }

                if (!Enum.TryParse<ProjectRole>(request.NewRole, true, out var role))
                {
                    return ApiResponse.Fail("Неверная роль");
                }

                member.RoleInProject = role;
                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    "Изменение роли",
                    $"Ваша роль в проекте изменена на {role}",
                    "Info",
                    $"/projects/{projectId}",
                    projectId,
                    "Project"
                );

                return ApiResponse.Ok("Роль участника обновлена");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка изменения роли участника в проекте {ProjectId}", projectId);
                return ApiResponse.Fail("Произошла ошибка при изменении роли");
            }
        }

        public async Task<ApiResponse> RemoveMemberAsync(Guid projectId, RemoveMemberRequest request)
        {
            try
            {
                var member = await _context.ProjectMembers
                    .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == request.UserId);

                if (member == null)
                {
                    return ApiResponse.Fail("Участник не найден");
                }

                _context.ProjectMembers.Remove(member);
                await _context.SaveChangesAsync();

                return ApiResponse.Ok("Участник удален");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления участника из проекта {ProjectId}", projectId);
                return ApiResponse.Fail("Произошла ошибка при удалении участника");
            }
        }

        public async Task<ApiResponse<ProjectStatisticsResponse>> GetProjectStatisticsAsync(Guid projectId)
        {
            try
            {
                var backlogItems = await _context.BacklogItems
                    .Where(bi => bi.ProjectId == projectId)
                    .ToListAsync();

                var sprints = await _context.Sprints
                    .Where(s => s.ProjectId == projectId)
                    .ToListAsync();

                var members = await _context.ProjectMembers
                    .CountAsync(pm => pm.ProjectId == projectId);

                var totalTasks = backlogItems.Count;
                var completedTasks = backlogItems.Count(bi => bi.Status == BacklogItemStatus.Done);
                var inProgressTasks = backlogItems.Count(bi => bi.Status == BacklogItemStatus.InProgress);
                var totalStoryPoints = backlogItems.Sum(bi => bi.StoryPoints ?? 0);
                var completedStoryPoints = backlogItems
                    .Where(bi => bi.Status == BacklogItemStatus.Done)
                    .Sum(bi => bi.StoryPoints ?? 0);

                var statistics = new ProjectStatisticsResponse
                {
                    TotalMembers = members,
                    TotalSprints = sprints.Count,
                    ActiveSprints = sprints.Count(s => s.IsActive),
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    InProgressTasks = inProgressTasks,
                    TotalStoryPoints = (int)totalStoryPoints,
                    CompletedStoryPoints = (int)completedStoryPoints,
                    CompletionPercentage = totalTasks > 0 ? (double)completedTasks / totalTasks * 100 : 0
                };

                return ApiResponse<ProjectStatisticsResponse>.Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статистики проекта {ProjectId}", projectId);
                return ApiResponse<ProjectStatisticsResponse>.Fail("Произошла ошибка при получении статистики");
            }
        }

        public async Task<bool> HasPermissionAsync(Guid projectId, Guid userId, string requiredRole)
        {
            try
            {
                var member = await _context.ProjectMembers
                    .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);

                if (member == null)
                {
                    var project = await _context.Projects.FindAsync(projectId);
                    return project?.OwnerId == userId;
                }

                if (!Enum.TryParse<ProjectRole>(requiredRole, true, out var role))
                    return false;

                return member.RoleInProject <= role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки прав доступа");
                return false;
            }
        }

        #region Private Methods

        private async Task<ProjectResponse> MapToProjectResponse(Project project)
        {
            var membersCount = await _context.ProjectMembers
                .CountAsync(pm => pm.ProjectId == project.Id);

            var activeSprints = await _context.Sprints
                .CountAsync(s => s.ProjectId == project.Id && s.IsActive);

            var totalTasks = await _context.BacklogItems
                .CountAsync(bi => bi.ProjectId == project.Id);

            var completedTasks = await _context.BacklogItems
                .CountAsync(bi => bi.ProjectId == project.Id && bi.Status == BacklogItemStatus.Done);

            return new ProjectResponse
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                Owner = project.Owner != null ? new UserBriefResponse
                {
                    Id = project.Owner.Id,
                    FullName = project.Owner.FullName,
                    Username = project.Owner.Username,
                    Email = project.Owner.Email
                } : new UserBriefResponse { Id = Guid.Empty, FullName = "Неизвестный", Username = "unknown" },
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt,
                IsArchived = project.IsArchived,
                MembersCount = membersCount,
                ActiveSprintsCount = activeSprints,
                TotalTasksCount = totalTasks,
                CompletedTasksCount = completedTasks
            };
        }

        #endregion
    }
}