using Microsoft.EntityFrameworkCore;
using ProjectManagementAPI.DataBaseContext;
using ProjectManagementAPI.DTO.Common;
using ProjectManagementAPI.DTO.Requests;
using ProjectManagementAPI.DTO.Responses;
using ProjectManagementAPI.Interfaces;
using ProjectManagementAPI.Models;
using ProjectManagementAPI.Enums;
using System.Text;

namespace ProjectManagementAPI.Services
{
    public class ReportService : BaseService, IReportService
    {
        private readonly ContextDb _context;

        public ReportService(ContextDb context, ILogger<ReportService> logger) : base(logger)
        {
            _context = context;
        }

        public async Task<ApiResponse<SprintReportResponse>> GenerateSprintReportAsync(Guid sprintId)
        {
            try
            {
                var sprint = await _context.Sprints
                    .Include(s => s.BacklogItems)
                        .ThenInclude(bi => bi.Assignee)
                    .Include(s => s.BacklogItems)
                        .ThenInclude(bi => bi.SubTasks)
                    .FirstOrDefaultAsync(s => s.Id == sprintId);

                if (sprint == null)
                {
                    return ApiResponse<SprintReportResponse>.Fail("Спринт не найден");
                }

                var completedTasks = new List<BacklogItemReportItem>();
                var incompleteTasks = new List<BacklogItemReportItem>();

                foreach (var task in sprint.BacklogItems)
                {
                    var reportItem = new BacklogItemReportItem
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Type = task.Type.ToString(),
                        StoryPoints = task.StoryPoints,
                        EstimatedHours = task.EstimatedHours,
                        ActualHours = task.ActualHours,
                        Assignee = task.Assignee != null ? new UserBriefResponse
                        {
                            Id = task.Assignee.Id,
                            FullName = task.Assignee.FullName,
                            Username = task.Assignee.Username
                        } : null,
                        CompletedAt = task.CompletedAt,
                        SubTasks = task.SubTasks.Select(st => new SubTaskBriefResponse
                        {
                            Id = st.Id,
                            Title = st.Title,
                            Status = st.Status.ToString(),
                            Assignee = st.Assignee != null ? new UserBriefResponse
                            {
                                Id = st.Assignee.Id,
                                FullName = st.Assignee.FullName,
                                Username = st.Assignee.Username
                            } : null,
                            EstimatedHours = st.EstimatedHours
                        }).ToList()
                    };

                    if (task.Status == BacklogItemStatus.Done)
                    {
                        completedTasks.Add(reportItem);
                    }
                    else
                    {
                        incompleteTasks.Add(reportItem);
                    }
                }

                var metrics = new SprintMetrics
                {
                    Velocity = sprint.CompletedStoryPoints ?? 0,
                    TasksByStatus = sprint.BacklogItems
                        .GroupBy(bi => bi.Status.ToString())
                        .ToDictionary(g => g.Key, g => g.Count()),
                    TasksByAssignee = sprint.BacklogItems
                        .Where(bi => bi.Assignee != null)
                        .GroupBy(bi => bi.Assignee!.FullName)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                var response = new SprintReportResponse
                {
                    Sprint = new SprintResponse
                    {
                        Id = sprint.Id,
                        Name = sprint.Name,
                        Goal = sprint.Goal,
                        StartDate = sprint.StartDate,
                        EndDate = sprint.EndDate,
                        IsActive = sprint.IsActive,
                        Status = sprint.Status.ToString(),
                        CreatedAt = sprint.CreatedAt,
                        TotalTasksCount = sprint.BacklogItems.Count,
                        CompletedTasksCount = sprint.BacklogItems.Count(bi => bi.Status == BacklogItemStatus.Done),
                        TotalStoryPoints = sprint.BacklogItems.Sum(bi => bi.StoryPoints),
                        CompletedStoryPoints = sprint.BacklogItems.Where(bi => bi.Status == BacklogItemStatus.Done).Sum(bi => bi.StoryPoints),
                        CompletionPercentage = sprint.BacklogItems.Any()
                            ? (double)sprint.BacklogItems.Count(bi => bi.Status == BacklogItemStatus.Done) / sprint.BacklogItems.Count * 100
                            : 0,
                        DaysRemaining = (sprint.EndDate - DateTime.UtcNow.Date).Days,
                        CommittedStoryPoints = sprint.CommittedStoryPoints,
                        CompletedStoryPointsModel = sprint.CompletedStoryPoints,
                        CompletedAt = sprint.CompletedAt,
                        ReviewNotes = sprint.ReviewNotes,
                        RetrospectiveNotes = sprint.RetrospectiveNotes
                    },
                    CompletedTasks = completedTasks,
                    IncompleteTasks = incompleteTasks,
                    Metrics = metrics,
                    ReviewNotes = sprint.ReviewNotes,
                    RetrospectiveNotes = sprint.RetrospectiveNotes
                };

                return ApiResponse<SprintReportResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации отчета по спринту {SprintId}", sprintId);
                return ApiResponse<SprintReportResponse>.Fail("Произошла ошибка при формировании отчета");
            }
        }

        public async Task<ApiResponse<TeamPerformanceReportResponse>> GenerateTeamPerformanceReportAsync(GenerateReportRequest request)
        {
            try
            {
                var project = await _context.Projects.FindAsync(request.ProjectId);
                if (project == null)
                {
                    return ApiResponse<TeamPerformanceReportResponse>.Fail("Проект не найден");
                }

                var tasks = await _context.BacklogItems
                    .Include(bi => bi.Assignee)
                    .Where(bi => bi.ProjectId == request.ProjectId &&
                                 bi.CreatedAt >= request.StartDate &&
                                 bi.CreatedAt <= request.EndDate)
                    .ToListAsync();

                var members = await _context.ProjectMembers
                    .Where(pm => pm.ProjectId == request.ProjectId)
                    .Include(pm => pm.User)
                    .ToListAsync();

                var teamMembersPerformance = new List<TeamMemberPerformance>();

                foreach (var member in members)
                {
                    var memberTasks = tasks.Where(t => t.AssigneeId == member.UserId).ToList();
                    var completedTasks = memberTasks.Where(t => t.Status == BacklogItemStatus.Done).ToList();

                    teamMembersPerformance.Add(new TeamMemberPerformance
                    {
                        User = new UserBriefResponse
                        {
                            Id = member.User.Id,
                            FullName = member.User.FullName,
                            Username = member.User.Username,
                            Email = member.User.Email
                        },
                        TasksCompleted = completedTasks.Count,
                        TasksInProgress = memberTasks.Count(t => t.Status == BacklogItemStatus.InProgress),
                        TotalStoryPointsCompleted = (int)completedTasks.Sum(t => t.StoryPoints ?? 0),
                        TotalEstimatedHours = memberTasks.Sum(t => t.EstimatedHours ?? 0),
                        TotalActualHours = memberTasks.Sum(t => t.ActualHours ?? 0),
                        Efficiency = memberTasks.Sum(t => t.EstimatedHours ?? 0) > 0
                            ? (double)(memberTasks.Sum(t => t.EstimatedHours ?? 0) / (memberTasks.Sum(t => t.ActualHours ?? 0) + 0.01m)) * 100
                            : 0,
                        CompletionRate = memberTasks.Any()
                            ? (double)completedTasks.Count / memberTasks.Count * 100
                            : 0,
                        CompletedTasks = completedTasks.Select(t => new BacklogItemReportItem
                        {
                            Id = t.Id,
                            Title = t.Title,
                            Type = t.Type.ToString(),
                            StoryPoints = t.StoryPoints,
                            EstimatedHours = t.EstimatedHours,
                            ActualHours = t.ActualHours,
                            CompletedAt = t.CompletedAt
                        }).ToList()
                    });
                }

                var aggregateMetrics = new TeamAggregateMetrics
                {
                    TotalTasksCompleted = teamMembersPerformance.Sum(m => m.TasksCompleted),
                    TotalStoryPointsCompleted = teamMembersPerformance.Sum(m => m.TotalStoryPointsCompleted),
                    TotalEstimatedHours = teamMembersPerformance.Sum(m => m.TotalEstimatedHours),
                    TotalActualHours = teamMembersPerformance.Sum(m => m.TotalActualHours),
                    OverallEfficiency = teamMembersPerformance.Any()
                        ? teamMembersPerformance.Average(m => m.Efficiency)
                        : 0,
                    AverageTasksPerMember = teamMembersPerformance.Any()
                        ? teamMembersPerformance.Average(m => m.TasksCompleted)
                        : 0
                };

                var response = new TeamPerformanceReportResponse
                {
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    TeamMembers = teamMembersPerformance,
                    AggregateMetrics = aggregateMetrics
                };

                return ApiResponse<TeamPerformanceReportResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации отчета по производительности команды");
                return ApiResponse<TeamPerformanceReportResponse>.Fail("Произошла ошибка при формировании отчета");
            }
        }

        public async Task<ApiResponse<VelocityReportResponse>> GenerateVelocityReportAsync(Guid projectId, int lastSprintsCount = 5)
        {
            try
            {
                var sprints = await _context.Sprints
                    .Where(s => s.ProjectId == projectId && s.Status == SprintStatus.Completed)
                    .OrderByDescending(s => s.EndDate)
                    .Take(lastSprintsCount)
                    .ToListAsync();

                var velocities = new List<SprintVelocityHistory>();

                foreach (var sprint in sprints)
                {
                    var tasks = await _context.BacklogItems
                        .Where(bi => bi.SprintId == sprint.Id)
                        .ToListAsync();

                    velocities.Add(new SprintVelocityHistory
                    {
                        SprintId = sprint.Id,
                        SprintName = sprint.Name,
                        EndDate = sprint.EndDate,
                        TotalStoryPoints = sprint.CommittedStoryPoints ?? 0,
                        CompletedStoryPoints = sprint.CompletedStoryPoints ?? 0,
                        Velocity = sprint.CompletedStoryPoints ?? 0,
                        CommittedTasks = tasks.Count,
                        CompletedTasks = tasks.Count(t => t.Status == BacklogItemStatus.Done)
                    });
                }

                var velocitiesList = velocities.Select(v => (decimal)v.Velocity).ToList();
                var sortedVelocities = velocitiesList.OrderBy(v => v).ToList();

                var response = new VelocityReportResponse
                {
                    SprintHistory = velocities,
                    AverageVelocity = velocitiesList.Any() ? velocitiesList.Average() : 0,
                    MedianVelocity = sortedVelocities.Any() ? sortedVelocities[sortedVelocities.Count / 2] : 0,
                    MinVelocity = sortedVelocities.Any() ? sortedVelocities.First() : 0,
                    MaxVelocity = sortedVelocities.Any() ? sortedVelocities.Last() : 0,
                    VelocityTrend = velocities.Count >= 2
                        ? (double)((velocities[0].Velocity - velocities[^1].Velocity) / (velocities[^1].Velocity + 0.01m)) * 100
                        : 0,
                    BurndownData = new List<BurndownPoint>()
                };

                return ApiResponse<VelocityReportResponse>.Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации Velocity отчета для проекта {ProjectId}", projectId);
                return ApiResponse<VelocityReportResponse>.Fail("Произошла ошибка при формировании отчета");
            }
        }

        public async Task<byte[]> ExportReportAsync(GenerateReportRequest request)
        {
            try
            {
                object reportData = request.Type switch
                {
                    ReportType.SprintReport when request.SprintId.HasValue =>
                        await GenerateSprintReportAsync(request.SprintId.Value),
                    ReportType.TeamPerformance =>
                        await GenerateTeamPerformanceReportAsync(request),
                    ReportType.VelocityReport =>
                        await GenerateVelocityReportAsync(request.ProjectId),
                    _ => await GenerateSprintReportAsync(request.SprintId ?? Guid.Empty)
                };

                return request.Format switch
                {
                    ReportFormat.CSV => GenerateCsvReport(reportData),
                    _ => GeneratePdfReport(reportData)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка экспорта отчета");
                return Array.Empty<byte>();
            }
        }

        #region Private Methods

        private byte[] GenerateCsvReport(object data)
        {
            try
            {
                var sb = new StringBuilder();

                if (data is ApiResponse<SprintReportResponse> sprintReport && sprintReport.Data != null)
                {
                    sb.AppendLine("Sprint Report");
                    sb.AppendLine($"Sprint: {sprintReport.Data.Sprint.Name}");
                    sb.AppendLine($"Period: {sprintReport.Data.Sprint.StartDate:d} - {sprintReport.Data.Sprint.EndDate:d}");
                    sb.AppendLine();
                    sb.AppendLine("Completed Tasks:");
                    sb.AppendLine("ID,Title,Type,Story Points,Estimated Hours,Actual Hours,Assignee,Completed At");

                    foreach (var task in sprintReport.Data.CompletedTasks)
                    {
                        sb.AppendLine($"{task.Id},{task.Title},{task.Type},{task.StoryPoints},{task.EstimatedHours},{task.ActualHours},{task.Assignee?.FullName},{task.CompletedAt:d}");
                    }
                }

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации CSV отчета");
                return Array.Empty<byte>();
            }
        }

        private byte[] GeneratePdfReport(object data)
        {
            // Заглушка для PDF
            return Array.Empty<byte>();
        }

        #endregion
    }
}