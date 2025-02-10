using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class Assignment
    {
        [Key]
        public int AssignmentId { get; set; }
        public int? LessonId { get; set; }

        [ForeignKey(nameof(LessonId))]
        public Lesson? Lesson { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public ICollection<MultipleChoiceQuestion> MultipleChoiceQuestions { get; set; } = new List<MultipleChoiceQuestion>();
        public ICollection<FillInBlankQuestion> FillInBlankQuestions { get; set; } = new List<FillInBlankQuestion>();
    }
}
