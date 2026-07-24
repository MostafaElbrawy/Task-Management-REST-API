using Task_Management.Enums;
using Task_Management.DTOs;


namespace Task_Management.Services
{
    public interface ITaskService
    {
        Task<ApiResponse<PagedList<TaskDto>>> GetProjectTasks(int projectId, int userId,
            Status? status, Priority? priority,
            DateTime? dueDateFrom, DateTime? dueDateTo,
            TaskSortColumn? sortColumn, SortOption? sortOption,
            int page, int pageSize);

        Task<ApiResponse<PagedList<TaskDto>>> GetAllTasks(int userId,
            string? searchTerm,
            Status? status, Priority? priority,
            DateTime? dueDateFrom, DateTime? dueDateTo,
            TaskSortColumn? sortColumn, SortOption? sortOption,
            int page, int pageSize);
        Task<ApiResponse<TaskDto?>> GetTask(int taskId, int userId);
        Task<ApiResponse<TaskDto?>> Create(CreateTaskDto createDto,int projectId ,int userId);
        Task<ApiResponse<TaskDto?>> Update(UpdateTaskDto updateDto, int taskId,int userId);
        Task<ApiResponse<bool>> Delete(int taskId,int userId);
    }
}
