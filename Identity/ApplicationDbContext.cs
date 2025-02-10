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
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<LessonContent> LessonContents { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<AssignmentResult> AssignmentResults { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<MultipleChoiceQuestion> MultipleChoiceQuestions { get; set; }
        public DbSet<FillInBlankQuestion> FillInBlankQuestions { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            const string adminRoleId = "b6b245a8-0a9d-4d1e-9c26-6bcbf27ef98a";
            const string lecturerRoleId = "c2b5a01e-6b30-4b7b-b450-27b9b39d31f7";
            const string userRoleId = "d0a3e13b-8659-4e70-a07e-9a8e2c5a7c45";

            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "a1a1a1a1-a1a1-4a1a-a1a1-a1a1a1a1a1a1" },
                new IdentityRole { Id = lecturerRoleId, Name = "Lecturer", NormalizedName = "LECTURER", ConcurrencyStamp = "b2b2b2b2-b2b2-4b2b-b2b2-b2b2b2b2b2b2" },
                new IdentityRole { Id = userRoleId, Name = "User", NormalizedName = "USER", ConcurrencyStamp = "c3c3c3c3-c3c3-4c3c-c3c3-c3c3c3c3c3c3" }
            );

            const string adminUserId = "a0d9ec33-7b25-4f40-9f4f-1e4d0f2e9842";
            var adminUser = new User
            {
                Id = adminUserId,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@ntt.com",
                FullName = "Admin",
                NormalizedEmail = "ADMIN@NTT.COM",
                EmailConfirmed = true,
                LockoutEnabled = false,
                SecurityStamp = "d4d4d4d4-d4d4-4d4d-d4d4-d4d4d4d4d4d4"
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
