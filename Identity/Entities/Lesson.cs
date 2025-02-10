using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class Lesson
    {
        [Key]
        public int LessonId { get; set; }

        [Required]
        public int? CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public ICollection<LessonContent>? LessonContents { get; set; }

        public Assignment Assignments { get; set; } = null!;
    }
}
