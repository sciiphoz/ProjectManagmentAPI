// Controllers/SprintsController.cs
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
    public class SprintsController : ControllerBase
    {
        private readonly ISprintService _sprintService;

        public SprintsController(ISprintService sprintService)
        {
            _sprintService = sprintService;
        }

        /// <summary>
        /// Создание спринта
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> CreateSprint(CreateSprintRequest request)
        {
            var response = await _sprintService.CreateSprintAsync(request);
            return CreatedAtAction(nameof(GetSprintById), new { sprintId = response.Data?.Id }, response);
        }

        /// <summary>
        /// Получение спринта по ID
        /// </summary>
        [HttpGet("{sprintId}")]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> GetSprintById(Guid sprintId)
        {
            var response = await _sprintService.GetSprintByIdAsync(sprintId);
            return Ok(response);
        }

        /// <summary>
        /// Получение всех спринтов проекта
        /// </summary>
        [HttpGet("project/{projectId}")]
        public async Task<ActionResult<ApiResponse<List<SprintBriefResponse>>>> GetProjectSprints(Guid projectId)
        {
            var response = await _sprintService.GetProjectSprintsAsync(projectId);
            return Ok(response);
        }

        /// <summary>
        /// Обновление спринта
        /// </summary>
        [HttpPut("{sprintId}")]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> UpdateSprint(Guid sprintId, UpdateSprintRequest request)
        {
            var response = await _sprintService.UpdateSprintAsync(sprintId, request);
            return Ok(response);
        }

        /// <summary>
        /// Запуск спринта (начало работы)
        /// </summary>
        [HttpPost("{sprintId}/start")]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> StartSprint(Guid sprintId, StartSprintRequest request)
        {
            request.SprintId = sprintId;
            var response = await _sprintService.StartSprintAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Завершение спринта
        /// </summary>
        [HttpPost("{sprintId}/complete")]
        public async Task<ActionResult<ApiResponse<SprintResponse>>> CompleteSprint(Guid sprintId, CompleteSprintRequest request)
        {
            request.SprintId = sprintId;
            var response = await _sprintService.CompleteSprintAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Отмена спринта
        /// </summary>
        [HttpPost("{sprintId}/cancel")]
        public async Task<ActionResult<ApiResponse>> CancelSprint(Guid sprintId)
        {
            var response = await _sprintService.CancelSprintAsync(sprintId);
            return Ok(response);
        }

        /// <summary>
        /// Удаление спринта
        /// </summary>
        [HttpDelete("{sprintId}")]
        public async Task<ActionResult<ApiResponse>> DeleteSprint(Guid sprintId)
        {
            var response = await _sprintService.DeleteSprintAsync(sprintId);
            return Ok(response);
        }

        /// <summary>
        /// Получение доски спринта (Scrum Board)
        /// </summary>
        [HttpGet("{sprintId}/board")]
        public async Task<ActionResult<ApiResponse<SprintBoardResponse>>> GetSprintBoard(Guid sprintId)
        {
            var response = await _sprintService.GetSprintBoardAsync(sprintId);
            return Ok(response);
        }

        /// <summary>
        /// Изменение статуса задачи на доске
        /// </summary>
        [HttpPatch("tasks/{taskId}/status")]
        public async Task<ActionResult<ApiResponse>> UpdateTaskStatus(Guid taskId, [FromBody] string newStatus)
        {
            var response = await _sprintService.UpdateTaskStatusAsync(taskId, newStatus);
            return Ok(response);
        }

        /// <summary>
        /// Перемещение задач в спринт (Sprint Planning)
        /// </summary>
        [HttpPost("move-to-sprint")]
        public async Task<ActionResult<ApiResponse>> MoveToSprint(MoveToSprintRequest request)
        {
            var response = await _sprintService.MoveToSprintAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Перемещение задачи обратно в бэклог
        /// </summary>
        [HttpPost("{backlogItemId}/move-to-backlog")]
        public async Task<ActionResult<ApiResponse>> MoveToBacklog(Guid backlogItemId)
        {
            var response = await _sprintService.MoveToBacklogAsync(backlogItemId);
            return Ok(response);
        }

        /// <summary>
        /// Получение метрик спринта
        /// </summary>
        [HttpGet("{sprintId}/metrics")]
        public async Task<ActionResult<ApiResponse<SprintMetrics>>> GetSprintMetrics(Guid sprintId)
        {
            var response = await _sprintService.GetSprintMetricsAsync(sprintId);
            return Ok(response);
        }

        /// <summary>
        /// Получение данных для Burndown Chart
        /// </summary>
        [HttpGet("{sprintId}/burndown")]
        public async Task<ActionResult<ApiResponse<List<BurndownPoint>>>> GetBurndownChart(Guid sprintId)
        {
            var response = await _sprintService.GetBurndownChartAsync(sprintId);
            return Ok(response);
        }

        /// <summary>
        /// Сохранение заметок Sprint Review
        /// </summary>
        [HttpPost("{sprintId}/review-notes")]
        public async Task<ActionResult<ApiResponse>> SaveReviewNotes(Guid sprintId, [FromBody] string notes)
        {
            var response = await _sprintService.SaveReviewNotesAsync(sprintId, notes);
            return Ok(response);
        }

        /// <summary>
        /// Сохранение заметок ретроспективы
        /// </summary>
        [HttpPost("{sprintId}/retrospective-notes")]
        public async Task<ActionResult<ApiResponse>> SaveRetrospectiveNotes(Guid sprintId, [FromBody] string notes)
        {
            var response = await _sprintService.SaveRetrospectiveNotesAsync(sprintId, notes);
            return Ok(response);
        }

        /// <summary>
        /// Получение истории спринтов для Velocity отчета
        /// </summary>
        [HttpGet("project/{projectId}/history")]
        public async Task<ActionResult<ApiResponse<List<SprintVelocityHistory>>>> GetSprintHistory(Guid projectId, [FromQuery] int count = 5)
        {
            var response = await _sprintService.GetSprintHistoryAsync(projectId, count);
            return Ok(response);
        }
    }
}