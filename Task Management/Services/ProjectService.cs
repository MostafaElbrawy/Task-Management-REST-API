using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Task_Management.DTOs;
using Task_Management.Models;
using Task_Management.Repository;

namespace Task_Management.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IUnifOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        public ProjectService(IUnifOfWork unitOfWork , UserManager<ApplicationUser>  userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        private static ProjectDto ProjectToDto(Project project) =>
            new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            };
        
        public async Task<ApiResponse<PagedList<ProjectDto>>> PagedProjects(int page, int pageSize,int userId)
        {
            if (page < 1 || pageSize < 1)
                return ApiResponse<PagedList<ProjectDto>>.ValidationError(message:"Invalid page/size");

            var projectsQuery = _unitOfWork.Projects.GetAll()
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.Id) //for pagination
                .Select(p => ProjectToDto(p));
            var data = await PagedList<ProjectDto>.CreateAsync(projectsQuery, page, pageSize);
            return ApiResponse<PagedList<ProjectDto>>.Ok(data);
        }
        
        public async Task<ApiResponse<ProjectDto?>> Project(int projectId , int userId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project == null)
                return ApiResponse<ProjectDto?>.NotFound("Project not found");

            if (project.UserId != userId)
                return ApiResponse<ProjectDto?>.Forbid();

            var data = ProjectToDto(project);
            return ApiResponse<ProjectDto?>.Ok(data);
        }

        public async Task<ApiResponse<ProjectDto?>> Create(CreateUpdateProjectDto createDto,int userId)
        {
            var nameExists = await _unitOfWork.Projects.GetByNameAsync(createDto.Name) != null;

            if (nameExists)
                return ApiResponse<ProjectDto?>.ValidationError(message:"Project name aleardy exists");

            var project = new Project
            {
                Name = createDto.Name,
                Description = createDto.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UserId = userId,
            };
            bool result = await _unitOfWork.Projects.AddAsync(project);
            if (!result)
                return ApiResponse<ProjectDto?>.Fail("Error while adding project");
            await _unitOfWork.CommitAsync();
            var data = ProjectToDto(project);

            return ApiResponse<ProjectDto?>.Created(data);

        }

        public async Task<ApiResponse<ProjectDto?>> Update(CreateUpdateProjectDto updateDto , int projectId , int userId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if(project == null)
                return ApiResponse<ProjectDto?>.NotFound("Project not found");

            if (project.UserId != userId)
                return ApiResponse<ProjectDto?>.Forbid();

            if (updateDto.Name.ToLower() != project.Name.ToLower())
            {
                var nameExists = await _unitOfWork.Projects.GetByNameAsync(updateDto.Name) != null;
                if (nameExists)
                    return ApiResponse<ProjectDto?>.ValidationError(message: "Project name aleardy exists");
            }

            project.Name = updateDto.Name;
            project.Description = updateDto.Description;
            project.UpdatedAt = DateTime.UtcNow;

            bool result = await _unitOfWork.Projects.UpdateAsync(project);
            if (!result)
                return ApiResponse<ProjectDto?>.Fail("Error while adding project");

            await _unitOfWork.CommitAsync();
            var data = ProjectToDto(project);

            return ApiResponse<ProjectDto?>.Ok(data);

        }
    
        public async Task<ApiResponse<bool>> Delete(int projectId , int userId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);
            if (project == null)
                return ApiResponse<bool>.NotFound("Project not found");
            
            if (project.UserId != userId)
                return ApiResponse<bool>.Forbid();

            bool result = await _unitOfWork.Projects.DeleteAsync(projectId);
            if (!result)
                return ApiResponse<bool>.Fail("Error while deleting project");
            await _unitOfWork.CommitAsync();

            return ApiResponse<bool>.Ok(true);
        }
    }
}
