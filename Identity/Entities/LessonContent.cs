using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class LessonContent
    {
        [Key]
        public int LessonContentId { get; set; }

        [Required]
        public int LessonId { get; set; }

        [ForeignKey(nameof(LessonId))]
        public Lesson Lesson { get; set; } = null!;

        [MaxLength(50)]
        public string? MediaType { get; set; }

        [MaxLength(255)]
        public string? MediaUrl { get; set; }

        public string? Content { get; set; }
    }
}
