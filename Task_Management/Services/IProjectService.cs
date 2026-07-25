using Task_Management.DTOs;

namespace Task_Management.Services
{
    public interface IProjectService
    {
        Task<ApiResponse<PagedList<ProjectDto>>> PagedProjects(int page, int pageSize, int userId);
        Task<ApiResponse<ProjectDto?>> Project(int projectId , int userId);
        Task<ApiResponse<ProjectDto?>> Create(CreateUpdateProjectDto createDto,int userId);
        Task<ApiResponse<ProjectDto?>> Update(CreateUpdateProjectDto updatetDto , int projectDto ,int userId);
        Task<ApiResponse<bool>> Delete(int projectId , int userId);
    }
}
