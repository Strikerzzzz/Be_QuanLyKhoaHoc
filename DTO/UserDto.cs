using Be_QuanLyKhoaHoc.Identity.Entities;

namespace Be_QuanLyKhoaHoc.DTO
{
    // Thông tin người dùng trả ra API; không đưa PasswordHash, SecurityStamp, ConcurrencyStamp ra ngoài
    public record UserDto(
        string Id,
        string? UserName,
        string? Email,
        string? FullName,
        string? AvatarUrl,
        string? PhoneNumber,
        bool EmailConfirmed,
        DateTimeOffset? LockoutEnd,
        DateTime CreatedAt
    )
    {
        public static UserDto FromEntity(User user) => new(
            user.Id,
            user.UserName,
            user.Email,
            user.FullName,
            user.AvatarUrl,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.LockoutEnd,
            user.CreatedAt);
    }
}
