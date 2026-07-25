using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Task_Management.DTOs;
using Task_Management.Filters;
using Task_Management.Services;

namespace Task_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;

        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (request == null)
            {
                var errorResponse = ApiResponse<bool>.Fail("Request body cannot be empty");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }

            var response = await _accountService.Login(request);
            return StatusCode(response.StatusCode, response);
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(RegistrationRequest request)
        {
            if (request == null)
            {
                var errorResponse  = ApiResponse<bool>.Fail("Request body cannot be empty");
                return StatusCode(errorResponse.StatusCode, errorResponse);
            }
            var response = await _accountService.Register(request);
            return StatusCode(response.StatusCode, response);
        }
    }
}
