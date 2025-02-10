using Microsoft.AspNetCore.Identity;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? AvatarUrl { get; set; }

    }
}
