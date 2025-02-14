using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual User User { get; set; } = null!;
        [Required]
        public DateTime Expires { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        [StringLength(50)]
        public string CreatedByIp { get; set; } = string.Empty;

        public DateTime? Revoked { get; set; }

        [StringLength(50)]
        public string? RevokedByIp { get; set; }

        public string? ReplacedByToken { get; set; }

        public bool IsActive => Revoked == null && DateTime.UtcNow < Expires;
    }
}
