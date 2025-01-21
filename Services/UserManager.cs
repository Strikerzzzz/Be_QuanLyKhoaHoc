using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Be_QuanLyKhoaHoc.Services
{
    public class UserManager
    {
        private readonly UserManager<User> _userManager;
        public UserManager(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IdentityResult> CreateUserAsync(User user, string password)
        {
            return await _userManager.CreateAsync(user, password);
        }

        public async Task<User?> FindByUsernameAsync(string username)
        {
            return await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<IdentityResult> UpdateUserAsync(User user)
        {
            return await _userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> DeleteUserAsync(User user)
        {
            return await _userManager.DeleteAsync(user);
        }

        public async Task<bool> CheckPasswordAsync(User user, string password)
        {
            return await _userManager.CheckPasswordAsync(user, password);
        }

        public string HashPassword(User user, string password)
        {
            return _userManager.PasswordHasher.HashPassword(user, password);
        }

        public PasswordVerificationResult VerifyPassword(User user, string hashedPassword, string providedPassword)
        {
            return _userManager.PasswordHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        }

        public async Task<IdentityResult> AddToRoleAsync(User user, string role)
        {
            return await _userManager.AddToRoleAsync(user, role);
        }

        public async Task<IList<string>> GetRolesAsync(User user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<IdentityResult> RemoveFromRoleAsync(User user, string role)
        {
            return await _userManager.RemoveFromRoleAsync(user, role);
        }

    }
}
