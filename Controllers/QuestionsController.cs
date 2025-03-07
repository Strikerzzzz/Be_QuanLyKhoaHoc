using Be_QuanLyKhoaHoc.Enums;
using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Be_QuanLyKhoaHoc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleProject.Common;
using System.ComponentModel.DataAnnotations;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly QuestionService _questionService;

        public QuestionsController(ApplicationDbContext context, QuestionService questionService)
        {
            _context = context;
            _questionService = questionService;
        }


        public record QuestionDto(
            [param: Required]
            [param: MaxLength(3000)]
            string Content,

            [param: Required]
            QuestionType Type,

            int? AnswerGroupNumber,
            string? Choices,
            int? CorrectAnswerIndex,
            string? CorrectAnswer,
            int? AssignmentId,
            int? ExamId
        );
        public record MultipleChoiceQuestionImportDto(
        [param: Required]
        [param: MaxLength(3000)]
        string Content,

        [param: Required]
        string Choices,

        [param: Required]
        int CorrectAnswerIndex,

        [param: Required]
        int AnswerGroupNumber,

        int? AssignmentId,
        int? ExamId
    );

        [HttpGet("{entityType}/{entityId}/questions/{questionType:int}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<IEnumerable<object>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> GetQuestions(string entityType, int entityId, Enums.QuestionType questionType)
        {
            try
            {
                if (string.IsNullOrEmpty(entityType))
                    return BadRequest("entityType không hợp lệ.");

                string entityKey;
                if (entityType.ToLower() == "assignment")
                {
                    entityKey = "AssignmentId";
                    if (!await _questionService.CheckEntityExists<Assignment>(entityId, entityKey))
                        return NotFound(Result<object>.Failure(new[] { "Không tìm thấy assignment." }));
                }
                else if (entityType.ToLower() == "exam")
                {
                    entityKey = "ExamId";
                    if (!await _questionService.CheckEntityExists<Exam>(entityId, entityKey))
                        return NotFound(Result<object>.Failure(new[] { "Không tìm thấy exam." }));
                }
                else
                {
                    return BadRequest("entityType không hợp lệ.");
                }

                List<object> questions = questionType switch
                {
                    Enums.QuestionType.MultipleChoice => await _questionService.GetMultipleChoiceQuestions(entityId, entityKey),
                    Enums.QuestionType.FillInTheBlank => await _questionService.GetFillInBlankQuestions(entityId, entityKey),
                    _ => throw new ArgumentException("Loại câu hỏi không hợp lệ.")
                };

                if (questions == null || !questions.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { $"Không tìm thấy câu hỏi cho {entityType} này." }));
                }

                return Ok(Result<IEnumerable<object>>.Success(questions));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

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
                            AnswerGroupNumber = dto.AnswerGroupNumber ?? 0,
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

        [HttpPost("bulk-import/multiple-choice")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> BulkImportMultipleChoiceQuestions([FromBody] IEnumerable<MultipleChoiceQuestionImportDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest(Result<object>.Failure(new[] { "Không có dữ liệu import." }));
            }

            // Chuyển đổi danh sách DTO thành các thực thể MultipleChoiceQuestion
            var mcqEntities = dtos.Select(dto => new MultipleChoiceQuestion
            {
                Content = dto.Content,
                Choices = dto.Choices,
                CorrectAnswerIndex = dto.CorrectAnswerIndex,
                AnswerGroupNumber = dto.AnswerGroupNumber,
                AssignmentId = dto.AssignmentId,
                ExamId = dto.ExamId
            }).ToList();

            _context.Questions.AddRange(mcqEntities);
            await _context.SaveChangesAsync();

            return Ok(Result<string>.Success("Import câu hỏi trắc nghiệm thành công."));
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
                            mcq.AnswerGroupNumber = dto.AnswerGroupNumber ?? 0;
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
