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
    [Authorize]
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
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(Result<IEnumerable<User>>.Success(users));
        }

        // API: Get a single user by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(Result<object>.Failure(new[] { "User not found." }));
            }
            return Ok(Result<User>.Success(user));
        }

        // API: Create a new user
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            var result = await _userManager.CreateAsync(user, "Default@Password123");
            if (result.Succeeded)
            {
                return Ok(Result<User>.Success(user));
            }
            return BadRequest(Result<object>.Failure(result.Errors.Select(e => e.Description)));
        }

        // API: Update user
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] User user)
        {
            if (id != user.Id)
            {
                return BadRequest(Result<object>.Failure(new[] { "User ID mismatch." }));
            }

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(Result<string>.Success("User updated successfully."));
        }

        // API: Delete user
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(Result<object>.Failure(new[] { "User not found." }));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(Result<string>.Success("User deleted successfully."));
        }
    }
}
