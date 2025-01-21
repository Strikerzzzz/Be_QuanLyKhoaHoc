using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Identity;
namespace Be_QuanLyKhoaHoc.Services
{
    public class UserService
    {
        private readonly UserManager<User> _userManager;

        public UserService(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IdentityResult> RegisterUserAsync(string username, string email, string password)
        {
            var user = new User
            {
                UserName = username,
                Email = email
            };

            return await _userManager.CreateAsync(user, password);
        }
    }
}
