
using Microsoft.AspNetCore.Identity;
using Task_Management.Enums; 
using Task_Management.DTOs;
using Task_Management.Models;
using Task_Management.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Task_Management.Services
{
    public class TaskService : ITaskService
    {
        private readonly IUnifOfWork _unitfOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
         private readonly ILogger<TaskService> _logger;
        public TaskService(IUnifOfWork unifOfWork, UserManager<ApplicationUser> userManager, ILogger<TaskService> logger)
        {
            _unitfOfWork = unifOfWork;
            _userManager = userManager;
            _logger = logger;
        }

        const Status defaultStatus = Status.Todo;
        const Priority defaultPriority = Priority.Medium;
        private static TaskDto TaskToDto(TaskItem task , Project project) =>
            new TaskDto
            {
                Id = task.Id,
                ProjectName = project.Name,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
            };


        public async Task<ApiResponse<PagedList<TaskDto>>> GetProjectTasks(int projectId, int userId,
            Status? status, Priority? priority,
            DateTime? dueDateFrom, DateTime? dueDateTo,
            TaskSortColumn? sortColumn, SortOption? sortOption,
            int page, int pageSize)
            // filtering - sorting - projection - paging
        {
            if (page <= 0)
                return ApiResponse<PagedList<TaskDto>>.ValidationError(message: "Invalid page");
            if (status == Status.None)
                return ApiResponse<PagedList<TaskDto>>.ValidationError( message:"Invalid status value");
            if(priority == Priority.None)
                return ApiResponse<PagedList<TaskDto>>.ValidationError(message: "Invalid priority value");
            if(sortColumn == TaskSortColumn.None)
                return ApiResponse<PagedList<TaskDto>>.ValidationError(message: "Invalid sort column");
            if(sortOption == SortOption.None)
                return ApiResponse<PagedList<TaskDto>>.ValidationError(message: "Invalid sort option");


            var tasksQuery =  _unitfOfWork.Tasks.GetAll()
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId && t.Project.UserId == userId);

            var project = await _unitfOfWork.Projects.GetByIdAsync(projectId);
            if (project == null) return ApiResponse<PagedList<TaskDto>>.NotFound("Project not found");
            if (project.UserId != userId) return ApiResponse<PagedList<TaskDto>>.Forbid();


            tasksQuery = FilterTasks(tasksQuery, status, priority, dueDateFrom, dueDateTo);
            tasksQuery = SortTasks(tasksQuery, sortColumn, sortOption);
            var tasksDtoQuery = tasksQuery.Select(t => TaskToDto(t,t.Project));
            var data = await PagedList<TaskDto>.CreateAsync(tasksDtoQuery, page, pageSize);
            return ApiResponse<PagedList<TaskDto>>.Ok(data);
        }

        public async Task<ApiResponse<PagedList<TaskDto>>> GetAllTasks(int userId,
            string? searchTerm,
            Status? status, Priority? priority,
            DateTime? dueDateFrom, DateTime? dueDateTo,
            TaskSortColumn? sortColumn, SortOption? sortOption,
            int page, int pageSize)
        {
            if (page <= 0)
                return ApiResponse<PagedList<TaskDto>>.ValidationError(message: "Invalid page");

            if (status == Status.None)
                return ApiResponse<PagedList<TaskDto>>.ValidationError(message: "Invalid status value");
            if (priority == Priority.None)
                return ApiResponse<PagedList<TaskDto>>.ValidationError(message: "Invalid priority value");
            if (sortColumn == TaskSortColumn.None)
                return ApiResponse<PagedList<TaskDto>>.ValidationError(message: "Invalid sort column");
            if (sortOption == SortOption.None)
                return ApiResponse<PagedList<TaskDto>>.ValidationError(message: "Invalid sort option");
            
            var tasksQuery = _unitfOfWork.Tasks.GetAll()
                .AsNoTracking()
                .Where(t => t.Project.UserId == userId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                tasksQuery = SearchTasks(tasksQuery, searchTerm);
            }


            tasksQuery = FilterTasks(tasksQuery, status, priority, dueDateFrom, dueDateTo);
            tasksQuery = SortTasks(tasksQuery, sortColumn, sortOption);
            var tasksDtoQuery = tasksQuery.Select(t => TaskToDto(t, t.Project));
            var data = await PagedList<TaskDto>.CreateAsync(tasksDtoQuery, page, pageSize);
            return ApiResponse<PagedList<TaskDto>>.Ok(data);
        }

        public async Task<ApiResponse<TaskDto?>> GetTask(int taskId, int userId)
        {
            var task = await _unitfOfWork.Tasks.GetByIdWithProject(taskId);

            if (task == null)
                return ApiResponse<TaskDto?>.NotFound("Task not found");
            
            if (task.Project.UserId != userId)
                return ApiResponse<TaskDto?>.Forbid();

            var data = TaskToDto(task, task.Project);

            return ApiResponse<TaskDto?>.Ok(data);
        }

        public async Task<ApiResponse<TaskDto?>> Create(CreateTaskDto createDto,int projectId ,int userId)
        {


            var project = await _unitfOfWork.Projects.GetByIdAsync(projectId);
            if (project == null)
                return ApiResponse<TaskDto?>.NotFound("Project not found");
            
            if (project.UserId != userId)
                return ApiResponse<TaskDto?>.Forbid();

            var task = TaskItem.Create(createDto.Title,createDto.Description,
                createDto.Status ?? defaultStatus,createDto.Priority ?? defaultPriority,
                createDto.DueDate,project.Id);
             
            bool result =await  _unitfOfWork.Tasks.AddAsync(task);
            if (!result)
                return ApiResponse<TaskDto?>.Fail("Error while creating task");

            await _unitfOfWork.CommitAsync();
            var data = TaskToDto(task, project);
            return ApiResponse<TaskDto?>.Created(data);
        }

        public async Task<ApiResponse<TaskDto?>> Update(UpdateTaskDto updateDto, int taskId,int userId)
        {



            var task =await _unitfOfWork.Tasks.GetByIdWithProject(taskId);
            //include the project so we can if he try to update a task of his own project

            if (task == null)
                return ApiResponse<TaskDto?>.NotFound("Task not found");

            if(task.Project.UserId != userId)
                return ApiResponse<TaskDto?>.Forbid();

            if (updateDto.DueDate.HasValue
                        && updateDto.DueDate.Value.Date < DateTime.UtcNow.Date
                        && updateDto.DueDate != task.DueDate) 
            {
                return ApiResponse<TaskDto?>.ValidationError(message: "Due date cannot be set to a past date");
            }

            var newProject = await _unitfOfWork.Projects.GetByIdAsync(updateDto.ProjectId);
            if (newProject == null)
                return ApiResponse<TaskDto?>.NotFound("Project not found");

            if (newProject.UserId != userId)
                return ApiResponse<TaskDto?>.Forbid();

            if (task.Status == Status.Done && updateDto.Status == Status.Todo)
                _logger.LogWarning("Task {TaskId} status changed from Done to Todo.", task.Id);

            task.Update(updateDto.Title,updateDto.Description,
            updateDto.Status ?? defaultStatus , updateDto.Priority ?? defaultPriority,
            updateDto.DueDate,newProject.Id);

            bool result = await _unitfOfWork.Tasks.UpdateAsync(task);
            if (!result)
                return ApiResponse<TaskDto?>.Fail("Error while updating task");
            

            await _unitfOfWork.CommitAsync();
            var data = TaskToDto(task, newProject);
            return ApiResponse<TaskDto?>.Ok(data);
        }

        public async Task<ApiResponse<bool>> Delete(int taskId, int userId)
        {
            var task = await _unitfOfWork.Tasks.GetByIdWithProject(taskId);
            if (task == null)
                return ApiResponse<bool>.NotFound("Task not found");

            if (task.Project.UserId != userId)
                return ApiResponse<bool>.Forbid();

            bool result = await _unitfOfWork.Tasks.DeleteAsync(taskId);
            if(!result)
                return ApiResponse<bool>.Fail("Error while deleting task");
            await _unitfOfWork.CommitAsync();

            return ApiResponse<bool>.Ok(true);
        }

        private IQueryable<TaskItem> FilterTasks(IQueryable<TaskItem> tasksQuery
            ,Status? status, Priority? priority,
            DateTime? dueDateFrom, DateTime? dueDateTo)
        {
            if(status != null)
                tasksQuery = tasksQuery.Where(t => t.Status == status);
            if(priority != null)
                tasksQuery = tasksQuery.Where(t => t.Priority == priority);
            if(dueDateFrom != null)
                tasksQuery = tasksQuery.Where(t => t.DueDate >= dueDateFrom);
            if(dueDateTo != null)
                tasksQuery = tasksQuery.Where(t => t.DueDate <=  dueDateTo);
            return tasksQuery;
        }

        private IQueryable<TaskItem> SortTasks(IQueryable<TaskItem> tasksQuery ,
            TaskSortColumn? sortColumn, SortOption? sortOption )
        {
            Expression<Func<TaskItem, object>> keySelector = sortColumn switch
            {
                TaskSortColumn.DueDate => task => task.DueDate,
                TaskSortColumn.Priority => task => task.Priority,
                TaskSortColumn.CreatedAt => task => task.CreatedAt,
                _ => task => task.Id
            };

            if(sortOption == SortOption.Desc)
            {
                return tasksQuery.OrderByDescending(keySelector);
            }
            return tasksQuery.OrderBy(keySelector);
            
        }

        private IQueryable<TaskItem> SearchTasks(IQueryable<TaskItem> tasksQuery, string searchTerm)
        {
            tasksQuery = tasksQuery.Where(t =>
                t.Title.Contains(searchTerm) ||
                (t.Description != null && t.Description.Contains(searchTerm))
            );
            return tasksQuery;
        }

    }
}
