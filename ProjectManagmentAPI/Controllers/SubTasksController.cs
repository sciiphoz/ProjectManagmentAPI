// Controllers/SubTasksController.cs
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

namespace ProjectManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SubTasksController : ControllerBase
    {
        private readonly ISubTaskService _subTaskService;

        public SubTasksController(ISubTaskService subTaskService)
        {
            _subTaskService = subTaskService;
        }

        /// <summary>
        /// Создание подзадачи
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> CreateSubTask(CreateSubTaskRequest request)
        {
            var response = await _subTaskService.CreateSubTaskAsync(request);
            return CreatedAtAction(nameof(GetSubTaskById), new { subTaskId = response.Data?.Id }, response);
        }

        /// <summary>
        /// Получение подзадачи по ID
        /// </summary>
        [HttpGet("{subTaskId}")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> GetSubTaskById(Guid subTaskId)
        {
            var response = await _subTaskService.GetSubTaskByIdAsync(subTaskId);
            return Ok(response);
        }

        /// <summary>
        /// Получение всех подзадач родительской задачи
        /// </summary>
        [HttpGet("backlog-item/{backlogItemId}")]
        public async Task<ActionResult<ApiResponse<List<SubTaskResponse>>>> GetBacklogItemSubTasks(Guid backlogItemId)
        {
            var response = await _subTaskService.GetBacklogItemSubTasksAsync(backlogItemId);
            return Ok(response);
        }

        /// <summary>
        /// Обновление подзадачи
        /// </summary>
        [HttpPut("{subTaskId}")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> UpdateSubTask(Guid subTaskId, UpdateSubTaskRequest request)
        {
            var response = await _subTaskService.UpdateSubTaskAsync(subTaskId, request);
            return Ok(response);
        }

        /// <summary>
        /// Удаление подзадачи
        /// </summary>
        [HttpDelete("{subTaskId}")]
        public async Task<ActionResult<ApiResponse>> DeleteSubTask(Guid subTaskId)
        {
            var response = await _subTaskService.DeleteSubTaskAsync(subTaskId);
            return Ok(response);
        }

        /// <summary>
        /// Начало работы над подзадачей
        /// </summary>
        [HttpPost("{subTaskId}/start")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> StartSubTask(Guid subTaskId)
        {
            var request = new StartSubTaskRequest { SubTaskId = subTaskId };
            var response = await _subTaskService.StartSubTaskAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Завершение подзадачи
        /// </summary>
        [HttpPost("{subTaskId}/complete")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> CompleteSubTask(Guid subTaskId, CompleteSubTaskRequest request)
        {
            request.SubTaskId = subTaskId;
            var response = await _subTaskService.CompleteSubTaskAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Изменение статуса подзадачи
        /// </summary>
        [HttpPatch("{subTaskId}/status")]
        public async Task<ActionResult<ApiResponse<SubTaskResponse>>> ChangeStatus(Guid subTaskId, ChangeSubTaskStatusRequest request)
        {
            var response = await _subTaskService.ChangeStatusAsync(subTaskId, request);
            return Ok(response);
        }

        /// <summary>
        /// Переупорядочивание подзадач (drag-and-drop)
        /// </summary>
        [HttpPost("reorder")]
        public async Task<ActionResult<ApiResponse>> ReorderSubTasks(ReorderSubTasksRequest request)
        {
            var response = await _subTaskService.ReorderSubTasksAsync(request);
            return Ok(response);
        }

        /// <summary>
        /// Получение статистики по подзадачам
        /// </summary>
        [HttpGet("backlog-item/{backlogItemId}/statistics")]
        public async Task<ActionResult<ApiResponse<SubTaskStatisticsResponse>>> GetSubTaskStatistics(Guid backlogItemId)
        {
            var response = await _subTaskService.GetSubTaskStatisticsAsync(backlogItemId);
            return Ok(response);
        }

        /// <summary>
        /// Добавление блокера к подзадаче
        /// </summary>
        [HttpPost("{subTaskId}/blockers")]
        public async Task<ActionResult<ApiResponse<BlockerResponse>>> AddBlocker(Guid subTaskId, AddBlockerRequest request)
        {
            var response = await _subTaskService.AddBlockerToSubTaskAsync(subTaskId, request.Description, request.Severity);
            return Ok(response);
        }
    }
}