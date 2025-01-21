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

        public LoginUser(ApplicationDbContext context, IPasswordHasher<User> passwordHasher, TokenProvider tokenProvider)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _tokenProvider = tokenProvider;
        }

        public sealed record Request(string Email, string Password);

        public async Task<string> Handle(Request request)
        {
            User? user = await _context.Users.GetByEmail(request.Email);

            if (user is null || !user.EmailConfirmed)
            {
                throw new Exception("The user was not found or email is not confirmed.");
            }

            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new Exception("Password hash is missing for the user.");
            }

            var verified = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (verified != PasswordVerificationResult.Success)
            {
                throw new Exception("The password is incorrect");
            }

            string token = _tokenProvider.Create(user);

            return token;
        }
    }
}
