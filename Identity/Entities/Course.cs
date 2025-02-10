using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Net.Mime.MediaTypeNames;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public double? Price { get; set; }
        [MaxLength(50)]
        public string? Difficulty { get; set; }
        public string? Keywords { get; set; }
        public string? AvatarUrl { get; set; }

        [Required]
        public string? LecturerId { get; set; } = string.Empty;

        [ForeignKey(nameof(LecturerId))]
        public User? Lecturer { get; set; }

        public Exam Exam { get; set; } = null!;
    }
}
