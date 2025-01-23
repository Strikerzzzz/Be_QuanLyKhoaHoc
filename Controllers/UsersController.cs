using Be_QuanLyKhoaHoc.Identity.Entities;
using Be_QuanLyKhoaHoc.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Be_QuanLyKhoaHoc.Services;
using Microsoft.AspNetCore.Authorization;
using SampleProject.Common;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly UserService _userService;
        private readonly LoginUser _loginUser;
        public UsersController(UserManager<User> userManager, ApplicationDbContext context, UserService userService, LoginUser loginUser)
        {
            _userManager = userManager;
            _context = context;
            _userService = userService;
            _loginUser = loginUser;
        }
        // DTOs for requests
        public record RegisterRequest(string Username, string Email, string Password);
        public record LoginRequest(string Email, string Password);

        // POST: /users/register
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Result<object>.Failure(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
            }

            var result = await _userService.RegisterUserAsync(request.Username, request.Email, request.Password);

            if (result.Succeeded)
            {
                return Ok(Result<string>.Success("User registered successfully."));
            }

            return BadRequest(Result<object>.Failure(result.Errors.Select(e => e.Description)));
        }

        // POST: /users/login
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(object), 401)] // Unauthorized
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Result<object>.Failure(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));
            }

            try
            {
                var token = await _loginUser.Handle(new LoginUser.Request(request.Email, request.Password));
                return Ok(Result<string>.Success(token));
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        // API: Get all users
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(typeof(Result<IEnumerable<User>>), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 403)]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(Result<IEnumerable<User>>.Success(users));
        }
    }
}
