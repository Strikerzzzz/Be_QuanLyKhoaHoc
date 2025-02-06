using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Be_QuanLyKhoaHoc.Enums;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class FillInBlankQuestion : Question
    {
        [Required]
        [MaxLength(255)]
        public string CorrectAnswer { get; set; } = string.Empty;

        public FillInBlankQuestion()
        {
            Type = QuestionType.FillInTheBlank;
        }
    }
}
