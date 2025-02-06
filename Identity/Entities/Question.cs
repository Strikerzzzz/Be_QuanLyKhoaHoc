using Be_QuanLyKhoaHoc.Enums;
using System.ComponentModel.DataAnnotations;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class Question
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;
        [Required]
        public QuestionType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
