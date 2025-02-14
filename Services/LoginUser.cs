using Be_QuanLyKhoaHoc.Extensions;
using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using static Be_QuanLyKhoaHoc.Controllers.UsersController;

namespace Be_QuanLyKhoaHoc.Services
{
    public sealed class LoginUser
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly TokenProvider _tokenProvider;
        private readonly UserManager<User> _userManager;

        public LoginUser(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, TokenProvider tokenProvider, UserManager<User> userManager)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenProvider = tokenProvider;
            _userManager = userManager;
        }

        public sealed record Request(string Email, string Password, string IpAddress);
        public sealed record LoginResponse(string AccessToken, string RefreshToken);
        public async Task<LoginResponse> Handle(Request request)
        {
            User? user = await _context.Users.GetByEmailAsync(request.Email);

            if (user is null || !user.EmailConfirmed)
            {
                throw new Exception("Không tìm thấy người dùng hoặc email chưa được xác nhận.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new Exception($"Tài khoản của bạn đã bị khóa đến {TimeZoneInfo.ConvertTimeFromUtc(user.LockoutEnd.Value.UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")):yyyy-MM-dd HH:mm:ss}.");

            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new Exception("Thiếu mật khẩu đã mã hóa cho người dùng.");
            }

            var verified = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (verified != PasswordVerificationResult.Success)
            {
                await _userManager.AccessFailedAsync(user);

                if (await _userManager.IsLockedOutAsync(user))
                {
                    if (!user.LockoutEnabled)
                    {
                        await _userManager.SetLockoutEnabledAsync(user, true);
                    }

                    throw new Exception($"Quá nhiều lần đăng nhập thất bại. Tài khoản của bạn đã bị khóa đến {TimeZoneInfo.ConvertTimeFromUtc(user.LockoutEnd.Value.UtcDateTime, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")):yyyy-MM-dd HH:mm:ss}.");
                }

                throw new Exception("Mật khẩu không đúng.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            string accessToken = await _tokenProvider.Create(user);
            var refreshToken = _tokenProvider.CreateRefreshToken(user.Id.ToString(), request.IpAddress);

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            return new LoginResponse(accessToken, refreshToken.Token);
        }
        public async Task<LoginResponse> Refresh(string refreshToken, string ipAddress)
        {
            var storedToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (storedToken == null || !storedToken.IsActive)
            {
                throw new Exception("Refresh token không hợp lệ hoặc đã hết hạn.");
            }

            storedToken.Revoked = DateTime.UtcNow;
            storedToken.RevokedByIp = ipAddress;

            string newAccessToken = await _tokenProvider.Create(storedToken.User);

            var newRefreshToken = _tokenProvider.CreateRefreshToken(storedToken.UserId, ipAddress);
            storedToken.ReplacedByToken = newRefreshToken.Token;

            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            return new LoginResponse(newAccessToken, newRefreshToken.Token);
        }
    }
}
