using Be_QuanLyKhoaHoc.Enums;
using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleProject.Common;
using System.ComponentModel.DataAnnotations;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
    public class QuestionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public QuestionsController(ApplicationDbContext context)
        {
            _context = context;
        }


        public record QuestionDto(
            [param: Required]
            [param: MaxLength(3000)]
            string Content,

            [param: Required]
            QuestionType Type,

            string? Choices,
            int? CorrectAnswerIndex,
            string? CorrectAnswer,
            int? AssignmentId,
            int? ExamId
        );


        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> CreateQuestion([FromBody] QuestionDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray();
                return BadRequest(Result<object>.Failure(errors));
            }
            try
            {
                Question question;
                switch (dto.Type)
                {
                    case QuestionType.MultipleChoice:
                        if (string.IsNullOrWhiteSpace(dto.Choices) || dto.CorrectAnswerIndex == null)
                        {
                            return BadRequest(Result<object>.Failure(new[] { "Dữ liệu cho câu hỏi trắc nghiệm không hợp lệ." }));
                        }
                        question = new MultipleChoiceQuestion
                        {
                            Content = dto.Content,
                            Choices = dto.Choices,
                            CorrectAnswerIndex = dto.CorrectAnswerIndex.Value,
                            AssignmentId = dto.AssignmentId,
                            ExamId = dto.ExamId
                        };
                        break;

                    case QuestionType.FillInTheBlank:
                        if (string.IsNullOrWhiteSpace(dto.CorrectAnswer))
                        {
                            return BadRequest(Result<object>.Failure(new[] { "Dữ liệu cho câu hỏi điền từ không hợp lệ." }));
                        }
                        question = new FillInBlankQuestion
                        {
                            Content = dto.Content,
                            CorrectAnswer = dto.CorrectAnswer,
                            AssignmentId = dto.AssignmentId,
                            ExamId = dto.ExamId
                        };
                        break;

                    default:
                        return BadRequest(Result<object>.Failure(new[] { "Loại câu hỏi không được hỗ trợ." }));
                }

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Tạo câu hỏi thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<Question>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray();
                    return BadRequest(Result<object>.Failure(errors));
                }

                var question = await _context.Questions.FirstOrDefaultAsync(q => q.Id == id);
                if (question == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy câu hỏi." }));
                }

                // Cập nhật các trường chung.
                question.Content = dto.Content;
                question.AssignmentId = dto.AssignmentId;
                question.ExamId = dto.ExamId;

                // Xử lý riêng theo loại câu hỏi.
                switch (question.Type)
                {
                    case QuestionType.MultipleChoice:
                        if (question is MultipleChoiceQuestion mcq)
                        {
                            if (string.IsNullOrWhiteSpace(dto.Choices) || dto.CorrectAnswerIndex == null)
                            {
                                return BadRequest(Result<object>.Failure(new[] { "Dữ liệu cho câu hỏi trắc nghiệm không hợp lệ." }));
                            }
                            mcq.Choices = dto.Choices;
                            mcq.CorrectAnswerIndex = dto.CorrectAnswerIndex.Value;
                        }
                        break;

                    case QuestionType.FillInTheBlank:
                        if (question is FillInBlankQuestion fibq)
                        {
                            if (string.IsNullOrWhiteSpace(dto.CorrectAnswer))
                            {
                                return BadRequest(Result<object>.Failure(new[] { "Dữ liệu cho câu hỏi điền từ không hợp lệ." }));
                            }
                            fibq.CorrectAnswer = dto.CorrectAnswer;
                        }
                        break;
                }

                await _context.SaveChangesAsync();
                return Ok(Result<Question>.Success(question));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            try
            {
                var question = await _context.Questions.FirstOrDefaultAsync(q => q.Id == id);
                if (question == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy câu hỏi." }));
                }

                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();

                return Ok(Result<object>.Success(null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

    }
}
