using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Identity;
namespace Be_QuanLyKhoaHoc.Services
{
    public class UserService
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserService(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IdentityResult> RegisterUserAsync(string username, string email, string password)
        {
            var user = new User
            {
                UserName = username,
                Email = email
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                string defaultRole = "User";
                if (!await _roleManager.RoleExistsAsync(defaultRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(defaultRole));
                }
                await _userManager.AddToRoleAsync(user, defaultRole);
            }

            return result;
        }
    }
}
