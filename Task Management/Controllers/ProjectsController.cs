using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Task_Management.DTOs;
using Task_Management.Services;

namespace Task_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _projectService;
        public ProjectsController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaginatedProjects([FromQuery] int page,[FromQuery] int pageSize)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();
           
            var response = await _projectService.PagedProjects(page, pageSize,userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpGet("{projectId:int}")]
        [Authorize]
        public async Task<IActionResult> GetProject(int projectId)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();
           
            var response = await _projectService.Project(projectId ,userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateProject(CreateUpdateProjectDto createDto)
        {
            if (createDto == null)
            {
                var errorResponse = ApiResponse<bool>.Fail("Request body cannot be empty");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();

            var response = await _projectService.Create(createDto,userId);
            return StatusCode(response.StatusCode, response);

        }

        [HttpPut("{projectId:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateProject(CreateUpdateProjectDto updateDto,int projectId)
        {
            if (updateDto == null)
            {
                var errorResponse = ApiResponse<bool>.Fail("Request body cannot be empty");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }

            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();
            
            var response = await _projectService.Update(updateDto , projectId, userId);
            return StatusCode(response.StatusCode, response);
        }

        [HttpDelete("{projectId:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int userId))
                return Unauthorized();
            
            var response = await _projectService.Delete(projectId, userId);
            return StatusCode(response.StatusCode, response);
        }




    }
}
