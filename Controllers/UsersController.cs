using Be_QuanLyKhoaHoc.Identity.Entities;
using Be_QuanLyKhoaHoc.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SampleProject.Common;
using Be_QuanLyKhoaHoc.Services;
using Be_QuanLyKhoaHoc.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;

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
        private readonly IAppEmailSender _emailSender;
        public UsersController(UserManager<User> userManager, ApplicationDbContext context,
            UserService userService, LoginUser loginUser, IAppEmailSender emailSender)
        {
            _userManager = userManager;
            _context = context;
            _userService = userService;
            _loginUser = loginUser;
            _emailSender = emailSender;
        }
        // DTOs for requests
        public record RegisterRequest(string Username, string Email, string Password);
        public record LoginRequest(string Email, string Password);
        public record AssignRoleRequest(string Email, string Role);
        public record RemoveRoleRequest(string Email, string Role);
        public record ConfirmEmailRequest(string Email, string Token);

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
                    var user = await _userManager.FindByEmailAsync(request.Email);
                    if (user != null)
                    {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var encodedToken = WebUtility.UrlEncode(token);
                        var confirmationLink = $"http://localhost:4200/email-confirmation?email={request.Email}&token={encodedToken}";

                        await _emailSender.SendEmailAsync(request.Email, "Xác nhận email",
                         $"Vui lòng xác nhận email của bạn bằng cách nhấp vào liên kết: <a href=\"{confirmationLink}\">Xác nhận email</a>");
                    }
                    return Ok(Result<string>.Success("Người dùng đã được đăng ký thành công. Một email xác nhận đã được gửi."));
                }
                return BadRequest(Result<object>.Failure(result.Errors.Select(e => e.Description).ToArray()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
            }
        }

        // POST: /users/confirm-email
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 404)] // Not Found
        [ProducesResponseType(typeof(object), 500)] // Internal server error
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(Result<object>.Failure(new[] { "Email hoặc mã xác nhận không hợp lệ." }));
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound(Result<object>.Failure(new[] { "Không tìm thấy người dùng." }));
            }
            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return BadRequest(Result<object>.Failure(new[] { "Email đã được xác nhận." }));
            }

            var result = await _userManager.ConfirmEmailAsync(user, request.Token);
            if (result.Succeeded)
            {
                return Ok(Result<string>.Success("Email đã được xác nhận thành công."));
            }

            return BadRequest(Result<object>.Failure(result.Errors.Select(e => e.Description).ToArray()));

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
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
            }
        }

        // API: Get all users
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
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
                    return NotFound(Result<IEnumerable<User>>.Failure(new[] { "Không tìm thấy người dùng." }));
                }
                return Ok(Result<IEnumerable<User>>.Success(users));
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, Result<IEnumerable<User>>.Failure(new[] { dbEx.Message }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<IEnumerable<User>>.Failure(new[] { ex.Message }));
            }
        }
        // DELETE: /users/{id}
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 401)] // Unauthorized
        [ProducesResponseType(typeof(object), 403)] // Forbidden
        [ProducesResponseType(typeof(object), 500)] // Internal server error
        public async Task<IActionResult> DeleteUsers(string id)
        {
            try
            {
                // Sử dụng ExecuteDeleteAsync để xoá user (thao tác trên query)
                var result = await _context.Users.Where(x => x.Id == id).ExecuteDeleteAsync();

                if (result > 0)
                {
                    return Ok(Result<string>.Success("Người dùng đã được xóa thành công."));
                }
                else
                {
                    return StatusCode(500, Result<object>.Failure(new[] { "Thao tác xóa thất bại." }));
                }
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, Result<object>.Failure(new[] { dbEx.Message }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
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
                    return NotFound(Result<object>.Failure(new[] { $"Không tìm thấy người dùng với email: {request.Email}" }));
                }

                var result = await _userManager.AddToRoleAsync(user, request.Role);
                if (result.Succeeded)
                {
                    return Ok(Result<string>.Success($"Vai trò '{request.Role}' đã được gán cho người dùng {request.Email} thành công."));
                }

                return BadRequest(Result<object>.Failure(result.Errors.Select(e => e.Description).ToArray()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
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
                    return NotFound(Result<object>.Failure(new[] { $"Không tìm thấy người dùng với email: {request.Email}" }));
                }

                var result = await _userManager.RemoveFromRoleAsync(user, request.Role);
                if (result.Succeeded)
                {
                    return Ok(Result<string>.Success($"Vai trò '{request.Role}' đã được xóa khỏi người dùng {request.Email} thành công."));
                }

                return BadRequest(Result<object>.Failure(result.Errors.Select(e => e.Description).ToArray()));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
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
                return BadRequest(Result<object>.Failure(new[] { "Email là bắt buộc." }));
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return NotFound(Result<object>.Failure(new[] { $"Không tìm thấy người dùng với email: {email}" }));
                }

                var roles = await _userManager.GetRolesAsync(user);
                return Ok(Result<IEnumerable<string>>.Success(roles));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
            }
        }

        // POST: /users/lock
        [HttpPost("lock")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 401)] // Unauthorized
        [ProducesResponseType(typeof(object), 403)] // Forbidden
        [ProducesResponseType(typeof(object), 500)] // Internal server error
        public async Task<IActionResult> LockUser([FromQuery] string id, [FromQuery] int minutes)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(Result<object>.Failure(new[] { "Id người dùng là bắt buộc." }));
            }
            if (minutes <= 0)
            {
                return BadRequest(Result<object>.Failure(new[] { "Thời gian khóa (phút) phải lớn hơn 0." }));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(Result<object>.Failure(new[] { "Không tìm thấy người dùng." }));
            }

            // Tính toán thời gian kết thúc khóa dựa trên thời gian hiện tại (UTC)
            DateTimeOffset lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(minutes);
            var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

            if (result.Succeeded)
            {
                return Ok(Result<string>.Success($"Người dùng bị khóa cho đến {lockoutEnd.ToOffset(TimeSpan.FromHours(7)).LocalDateTime:yyyy-MM-dd HH:mm:ss}"));

            }
            else
            {
                return StatusCode(500, Result<object>.Failure(result.Errors.Select(e => e.Description).ToArray()));
            }
        }

        // POST: /users/unlock
        [HttpPost("unlock")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 401)] // Unauthorized
        [ProducesResponseType(typeof(object), 403)] // Forbidden
        [ProducesResponseType(typeof(object), 500)] // Internal server error
        public async Task<IActionResult> UnlockUser([FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(Result<object>.Failure(new[] { "Id người dùng là bắt buộc." }));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(Result<object>.Failure(new[] { "Không tìm thấy người dùng." }));
            }

            // Đặt LockoutEnd = null sẽ mở khóa tài khoản
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                return Ok(Result<string>.Success("Người dùng đã được mở khóa thành công."));
            }
            else
            {
                return StatusCode(500, Result<object>.Failure(result.Errors.Select(e => e.Description).ToArray()));
            }
        }
    }
}
