using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text.Json;
using Task_Management.DTOs;
using Task_Management.Enums;
using Task_Management.Models;
using Task_Management.Services;

namespace Task_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet("/api/projects/{projectId:int}/tasks")]
        public async Task<IActionResult> GetProjectTasks(int projectId,
            Status? status, Priority? priority,
            DateTime? dueDateFrom, DateTime? dueDateTo,
            TaskSortColumn? sortColumn, SortOption? sortOption,
            int page, int pageSize)
        {

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var response = await _taskService.GetProjectTasks(projectId,userId,status,priority,dueDateFrom
                ,dueDateTo,sortColumn,sortOption,page,pageSize);

            return StatusCode(response.StatusCode, response);
        }

        [HttpGet]
        
        public async Task<IActionResult> GetAllTasks([FromQuery(Name = "q")] string? searchTerm,
            Status? status, Priority? priority,
            DateTime? dueDateFrom, DateTime? dueDateTo,
            TaskSortColumn? sortColumn, SortOption? sortOption,
            int page, int pageSize)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var response = await _taskService.GetAllTasks( userId, searchTerm,status, priority, dueDateFrom
                , dueDateTo, sortColumn, sortOption, page, pageSize);

            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{taskId:int}")]
        public async Task<IActionResult> GetTask(int taskId)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var response = await _taskService.GetTask(taskId, userId);

            return StatusCode(response.StatusCode, response);
        }


        [HttpPost("/api/projects/{projectId:int}/tasks")]
        public async Task<IActionResult> CreateTask(CreateTaskDto createDto, int projectId)
        {
            if (createDto == null)
            {
                var errorResponse = ApiResponse<bool>.Fail("Request body cannot be empty");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var response = await _taskService.Create(createDto, projectId,userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPut("{taskId:int}")]
        public async Task<IActionResult> UpdateTask(UpdateTaskDto updateDto, int taskId)
        {
            if (updateDto == null)
            {
                var errorResponse = ApiResponse<bool>.Fail("Request body cannot be empty");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var response = await _taskService.Update(updateDto, taskId, userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{taskId:int}")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var response = await _taskService.Delete(taskId, userId);
            return StatusCode(response.StatusCode, response);
        }
    }
}
