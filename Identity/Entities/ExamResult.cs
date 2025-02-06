using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class ExamResult
    {
        [Key]
        public int ResultId { get; set; }
        public string? StudentId { get; set; } = string.Empty;

        [ForeignKey(nameof(StudentId))]
        public User? Student { get; set; }

        [Required]
        public int ExamId { get; set; }

        [ForeignKey(nameof(ExamId))]
        public Exam? Exam { get; set; }

        [Range(0, 100)]
        public float Score { get; set; }
        public DateTime? SubmissionTime { get; set; }
    }
}
