using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Be_QuanLyKhoaHoc.Enums;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class MultipleChoiceQuestion : Question
    {

        [Required]
        // Chuỗi JSON chứa danh sách các đáp án.
        public string Choices { get; set; } = string.Empty;
        [Required]
        [Range(0, int.MaxValue)]
        public int CorrectAnswerIndex { get; set; }
        [MaxLength(50)]
        public string Difficulty { get; set; } = "Normal";
        public MultipleChoiceQuestion()
        {
            Type = QuestionType.MultipleChoice;
        }
    }
}
