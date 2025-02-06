using Be_QuanLyKhoaHoc.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class Answer
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int SourceValue { get; set; }

        // Thuộc tính không được ánh xạ trực tiếp đến cơ sở dữ liệu, giúp chuyển đổi giữa giá trị số và enum QuestionSource.
        [NotMapped]
        public QuestionSource Source
        {
            get => (QuestionSource)SourceValue;
            set => SourceValue = (int)value;
        }

        public int? ExamId { get; set; }
        public Exam? Exam { get; set; }

        public int? AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }

        [Required]
        public int QuestionId { get; set; }
        public Question? Question { get; set; }

        /// <summary>
        /// Câu trả lời của học sinh dành cho câu hỏi trắc nghiệm (chỉ số đáp án).
        /// </summary>
        public int? UserAnswerIndex { get; set; }

        /// <summary>
        /// Câu trả lời của học sinh dành cho câu hỏi điền từ (văn bản).
        /// </summary>
        public string? UserAnswer { get; set; }

        public bool IsCorrect { get; set; }
    }
}
