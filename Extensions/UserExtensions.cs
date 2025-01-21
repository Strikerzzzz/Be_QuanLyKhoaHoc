using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace Be_QuanLyKhoaHoc.Extensions
{
    public static class UserExtensions
    {
        public static async Task<User> GetByEmail(this DbSet<User> users, string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email không được để trống.", nameof(email));
            }

            var user = await users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                throw new InvalidOperationException($"Không tìm thấy người dùng với email: {email}");
            }

            return user;
        }
    }
}
