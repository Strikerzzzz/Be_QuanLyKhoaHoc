using Be_QuanLyKhoaHoc.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(3000)]
        public string Content { get; set; } = string.Empty;

        [Required]
        public QuestionType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? AssignmentId { get; set; }
        [ForeignKey(nameof(AssignmentId))]
        public Assignment? Assignment { get; set; }

        public int? ExamId { get; set; }
        [ForeignKey(nameof(ExamId))]
        public Exam? Exam { get; set; }
    }
}
