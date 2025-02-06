using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class Progress
    {
        [Key]
        public int ProgressId { get; set; }

        [Required]
        public string? StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public User? Student { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }

        [Range(0, int.MaxValue)]
        public int CompletedLessons { get; set; } = 0;

        [Range(0, 100)]
        public float CompletionRate { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public float TotalAssignmentScore { get; set; } = 0;

        public float TotalExamScore { get; set; } = 0;
    }
}
