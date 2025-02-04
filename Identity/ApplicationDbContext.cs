using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Be_QuanLyKhoaHoc.Identity
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            var adminRoleId = Guid.NewGuid().ToString();
            var lecturerRoleId = Guid.NewGuid().ToString();
            var userRoleId = Guid.NewGuid().ToString();

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = Guid.NewGuid().ToString() },
                new IdentityRole { Id = lecturerRoleId, Name = "Lecturer", NormalizedName = "LECTURER", ConcurrencyStamp = Guid.NewGuid().ToString() },
                new IdentityRole { Id = userRoleId, Name = "User", NormalizedName = "USER", ConcurrencyStamp = Guid.NewGuid().ToString() }
            );

            var adminUserId = Guid.NewGuid().ToString();
            var adminUser = new User
            {
                Id = adminUserId,
                UserName = "admin",
                Email = "admin@ntt.com",
                FullName = "Admin",
                NormalizedUserName = "ADMIN@NTT.COM",
                NormalizedEmail = "ADMIN@NTT.COM",
                EmailConfirmed = true,
                LockoutEnabled = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var passwordHasher = new PasswordHasher<User>();
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "t12345678");

            builder.Entity<User>().HasData(adminUser);
            builder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { UserId = adminUserId, RoleId = adminRoleId },
                new IdentityUserRole<string> { UserId = adminUserId, RoleId = lecturerRoleId },
                new IdentityUserRole<string> { UserId = adminUserId, RoleId = userRoleId }
            );
        }
    }
}
