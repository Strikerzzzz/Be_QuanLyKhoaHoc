﻿using Be_QuanLyKhoaHoc.Identity.Entities;
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
using static Be_QuanLyKhoaHoc.Services.LoginUser;
using System.Globalization;
using System.Security.Claims;

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
        private readonly IConfiguration _configuration;
        public UsersController(UserManager<User> userManager, ApplicationDbContext context,
            UserService userService, LoginUser loginUser, IAppEmailSender emailSender, IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _userService = userService;
            _loginUser = loginUser;
            _emailSender = emailSender;
            _configuration = configuration;
        }
        // DTOs for requests
        public record RegisterRequest(string Username, string Email, string Password);
        public record LoginRequest(string Email, string Password);
        public record AssignRoleRequest(string Email, string Role);
        public record RemoveRoleRequest(string Email, string Role);
        public record ConfirmEmailRequest(string Email, string Token);
        public record RefreshTokenRequest(string RefreshToken);
        public record RefreshTokenResponse(string AccessToken, string RefreshToken);
        public record UserPagedResult(IEnumerable<User> Users, int TotalCount);
        public record UpdateUserProfileRequest(
        string? PhoneNumber,
        string? FullName,
        string? AvatarUrl
    );

        // POST: /users/register
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(Result<object>), 500)]  // Internal server error
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

                        var confirmationBaseUrl = _configuration["EmailSettings:ConfirmationUrl"];
                        var confirmationLink = $"{confirmationBaseUrl}?email={request.Email}&token={encodedToken}";

                        await _emailSender.SendEmailAsync(
                            request.Email,
                            "Xác nhận email",
                            $"Vui lòng xác nhận email của bạn bằng cách nhấp vào liên kết: <a href=\"{confirmationLink}\">Xác nhận email</a>"
                        );
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
        [ProducesResponseType(typeof(Result<object>), 404)] // Not Found
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
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(Result<object>), 500)] // Internal server error
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Result<object>.Failure(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray()));
            }

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var loginResult = await _loginUser.Handle(new Request(request.Email, request.Password, ipAddress));

                return Ok(Result<LoginResponse>.Success(loginResult));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
            }
        }

        // --- Endpoint refresh token ---
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<RefreshTokenResponse>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(Result<object>.Failure(ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray()));
            }

            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var loginResponse = await _loginUser.Refresh(request.RefreshToken, ipAddress);
                var response = new RefreshTokenResponse(loginResponse.AccessToken, loginResponse.RefreshToken);
                return Ok(Result<RefreshTokenResponse>.Success(response));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
            }
        }

        // API: Get all users
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [HttpGet]
        [ProducesResponseType(typeof(Result<UserPagedResult>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string options = null)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                // Xây dựng truy vấn với điều kiện tìm kiếm nếu có
                var query = _context.Users.AsNoTracking()
                    .Where(u => string.IsNullOrEmpty(options) ||
                                EF.Functions.Like(u.FullName, $"%{options}%") ||
                                EF.Functions.Like(u.UserName, $"%{options}%") ||
                                EF.Functions.Like(u.Email, $"%{options}%") ||
                                EF.Functions.Like(u.PhoneNumber, $"%{options}%"));

                // Lấy tổng số bản ghi theo điều kiện tìm kiếm
                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy người dùng." }));
                }

                // Tính tổng số trang
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Đảm bảo page không vượt quá tổng số trang
                if (page > totalPages) page = totalPages;

                // Lấy dữ liệu theo phân trang
                var users = await query
                    .OrderBy(u => u.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                if (users == null || !users.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy người dùng." }));
                }

                var result = new UserPagedResult(users, totalCount);

                return Ok(Result<UserPagedResult>.Success(result));
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

        // DELETE: /users/{id}
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success
        [ProducesResponseType(typeof(object), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 401)] // Unauthorized
        [ProducesResponseType(typeof(object), 403)] // Forbidden
        [ProducesResponseType(typeof(Result<object>), 500)] // Internal server error
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
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)] // Internal server error
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
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)] // Internal server error
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
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)] // Internal server error
        public async Task<IActionResult> GetUserRoles(string email)
        {
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
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
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
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)] // Internal server error
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

        [HttpGet("user-statistics")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<object>), 200)] // Success
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(object), 401)] // Unauthorized
        [ProducesResponseType(typeof(object), 403)] // Forbidden
        [ProducesResponseType(typeof(Result<object>), 500)] // Internal server error
        public async Task<IActionResult> GetUserStatistics([FromQuery] string period)
        {
            try
            {
                var now = DateTime.UtcNow;
                IQueryable<User> query = _context.Users;

                object statistics = null;

                switch (period?.ToLower())
                {
                    case "days":
                        var tenDaysAgo = now.Date.AddDays(-9); // Lấy 10 ngày gần nhất
                        statistics = await query
                            .Where(u => u.CreatedAt >= tenDaysAgo)
                            .GroupBy(u => u.CreatedAt.Date)
                            .Select(g => new { Date = g.Key, Count = g.Count() })
                            .OrderBy(g => g.Date)
                            .ToListAsync();
                        break;

                    case "months":
                        var twelveMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-11); // Lấy 12 tháng gần nhất
                        statistics = await query
                            .Where(u => u.CreatedAt >= twelveMonthsAgo)
                            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                            .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                            .OrderBy(g => g.Year).ThenBy(g => g.Month)
                            .ToListAsync();
                        break;

                    case "years":
                        var fiveYearsAgo = new DateTime(now.Year - 4, 1, 1); // Lấy 5 năm gần nhất
                        statistics = await query
                            .Where(u => u.CreatedAt >= fiveYearsAgo)
                            .GroupBy(u => u.CreatedAt.Year)
                            .Select(g => new { Year = g.Key, Count = g.Count() })
                            .OrderBy(g => g.Year)
                            .ToListAsync();
                        break;

                    default:
                        return BadRequest(Result<object>.Failure(new[] { "Tham số period không hợp lệ. Chọn 'days', 'months', hoặc 'years'." }));
                }

                return Ok(Result<object>.Success(statistics));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
            }

        }

        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [HttpGet("profile")]
        [ProducesResponseType(typeof(Result<User>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(Result<object>.Failure(new[] { "Không có thông tin người dùng." }));
                }

                var user = await _context.Users.AsNoTracking()
                             .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

                if (user == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy người dùng." }));
                }

                return Ok(Result<User>.Success(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
            }
        }
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [HttpPut("profile")]
        [ProducesResponseType(typeof(Result<User>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileRequest request)
        {
            try
            {
                // Lấy giá trị ID của người dùng từ token
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(Result<object>.Failure(new[] { "Không có thông tin người dùng." }));
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);
                if (user == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy người dùng." }));
                }

                if (request.PhoneNumber != null)
                {
                    user.PhoneNumber = request.PhoneNumber;
                }
                if (request.FullName != null)
                {
                    user.FullName = request.FullName;
                }
                if (request.AvatarUrl != null)
                {
                    user.AvatarUrl = request.AvatarUrl;
                }

                // Lưu thay đổi vào database
                await _context.SaveChangesAsync();

                return Ok(Result<User>.Success(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { ex.Message }));
            }
        }
    }
}
