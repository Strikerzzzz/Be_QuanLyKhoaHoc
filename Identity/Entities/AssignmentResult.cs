using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Be_QuanLyKhoaHoc.Identity.Entities
{
    public class AssignmentResult
    {
        [Key]
        public int ResultId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [ForeignKey(nameof(StudentId))]
        public User? Student { get; set; }

        [Required]
        public int AssignmentId { get; set; }

        [ForeignKey(nameof(AssignmentId))]
        public Assignment? Assignment { get; set; }

        [Range(0, 100)]
        public float Score { get; set; }
        public DateTime? SubmissionTime { get; set; }
    }
}
