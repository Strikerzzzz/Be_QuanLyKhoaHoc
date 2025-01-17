using Microsoft.EntityFrameworkCore;

namespace Be_QuanLyKhoaHoc.Identity
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

    }
}
