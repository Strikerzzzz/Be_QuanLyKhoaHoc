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
        public record AssignRoleRequest(string Email, string Role);
        public record RemoveRoleRequest(string Email, string Role);
        // POST: /users/register
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 500)]  // Internal server error
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Result<object>.Failure(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray()));
            }

            try
            {
                var result = await _userService.RegisterUserAsync(request.Username, request.Email, request.Password);

                if (result.Succeeded)
                {
                    return Ok(Result<string>.Success("User registered successfully."));
                }

                return BadRequest(Result<object>.Failure(result.Errors.Select(e => e.Description).ToArray()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { "An unexpected error occurred.", ex.Message }));
            }
        }

        // POST: /users/login
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(object), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 500)] // Internal server error
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Result<object>.Failure(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray()));
            }

            try
            {
                var token = await _loginUser.Handle(new LoginUser.Request(request.Email, request.Password));
                return Ok(Result<string>.Success(token));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { "An unexpected error occurred.", ex.Message }));
            }
        }

        // API: Get all users
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [HttpGet]
        [ProducesResponseType(typeof(Result<IEnumerable<User>>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users.ToListAsync();
                if (users == null || !users.Any())
                {
                    return NotFound(Result<IEnumerable<User>>.Failure(new[] { "No users found." }));
                }
                return Ok(Result<IEnumerable<User>>.Success(users));
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, Result<IEnumerable<User>>.Failure(new[] { "Database error occurred.", dbEx.Message }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<IEnumerable<User>>.Failure(new[] { "An unexpected error occurred.", ex.Message }));
            }
        }

        // POST: /users/assign-role
        [HttpPost("assign-role")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 401)] // Unauthorized
        [ProducesResponseType(typeof(object), 403)] // Forbidden
        [ProducesResponseType(typeof(object), 500)] // Internal server error
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Result<object>.Failure(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray()));
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return NotFound(Result<object>.Failure(new[] { $"No user found with the email: {request.Email}" }));
                }

                var result = await _userManager.AddToRoleAsync(user, request.Role);
                if (result.Succeeded)
                {
                    return Ok(Result<string>.Success($"Role '{request.Role}' has been assigned to user {request.Email} successfully."));
                }

                return BadRequest(Result<object>.Failure(result.Errors.Select(e => e.Description).ToArray()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { "An unexpected error occurred.", ex.Message }));
            }
        }

        // POST: /users/remove-role
        [HttpPost("remove-role")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 401)] // Unauthorized
        [ProducesResponseType(typeof(object), 403)] // Forbidden
        [ProducesResponseType(typeof(object), 500)] // Internal server error
        public async Task<IActionResult> RemoveRole([FromBody] RemoveRoleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Result<object>.Failure(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray()));
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return NotFound(Result<object>.Failure(new[] { $"No user found with the email: {request.Email}" }));
                }

                var result = await _userManager.RemoveFromRoleAsync(user, request.Role);
                if (result.Succeeded)
                {
                    return Ok(Result<string>.Success($"Role '{request.Role}' has been removed from user {request.Email} successfully."));
                }

                return BadRequest(Result<object>.Failure(result.Errors.Select(e => e.Description).ToArray()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { "An unexpected error occurred.", ex.Message }));
            }
        }

        // GET: /users/roles/{email}
        [HttpGet("roles/{email}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<IEnumerable<string>>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 401)] // Unauthorized
        [ProducesResponseType(typeof(object), 403)] // Forbidden
        [ProducesResponseType(typeof(object), 500)] // Internal server error
        public async Task<IActionResult> GetUserRoles(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(Result<object>.Failure(new[] { "Email is required." }));
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(Result<object>.Failure(new[] { $"No user found with the email: {email}" }));
                }

                var roles = await _userManager.GetRolesAsync(user);
                return Ok(Result<IEnumerable<string>>.Success(roles));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { "An unexpected error occurred.", ex.Message }));
            }
        }
    }
}
