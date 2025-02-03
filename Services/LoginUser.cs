using Be_QuanLyKhoaHoc.Extensions;
using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using System;

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

        public sealed record Request(string Email, string Password);

        public async Task<string> Handle(Request request)
        {
            User? user = await _context.Users.GetByEmailAsync(request.Email);

            if (user is null || !user.EmailConfirmed)
            {
                throw new Exception("The user was not found or email is not confirmed.");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new Exception($"Your account is locked until {user.LockoutEnd?.ToLocalTime():yyyy-MM-dd HH:mm:ss}.");
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new Exception("Password hash is missing for the user.");
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

                    throw new Exception($"Too many failed attempts. Your account has been locked until {user.LockoutEnd?.ToLocalTime():yyyy-MM-dd HH:mm:ss}.");
                }

                throw new Exception("The password is incorrect.");
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            string token = await _tokenProvider.Create(user);
            return token;
        }
    }
}
