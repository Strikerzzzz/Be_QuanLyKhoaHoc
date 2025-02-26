using Be_QuanLyKhoaHoc.Enums;

namespace Be_QuanLyKhoaHoc.DTO
{
    public record QuestionPreviewDto(
        int Id,
        string Content,
        QuestionType Type,
        DateTime CreatedAt,
        string? Choices,
        int? CorrectAnswerIndex,
        string? CorrectAnswer
    );
}
