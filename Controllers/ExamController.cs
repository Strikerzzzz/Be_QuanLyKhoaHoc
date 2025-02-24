using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Be_QuanLyKhoaHoc.Identity.Entities;
using SampleProject.Common;
using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Enums;
using Be_QuanLyKhoaHoc.DTO;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
    public class ExamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("course/{courseId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> GetExamByCourse(int courseId)
        {
            try
            {
                var courseExists = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.CourseId == courseId);
                if (!courseExists)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                var exam = await _context.Exams
                    .AsNoTracking()
                    .Where(e => e.CourseId == courseId)
                    .Select(e => new { e.ExamId, e.Title, e.Description, e.RandomMultipleChoiceCount })
                    .SingleOrDefaultAsync();

                if (exam == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra cho khóa học này." }));
                }

                return Ok(Result<object>.Success(exam));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        [HttpGet("{examId}/questions")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<IEnumerable<QuestionPreviewDto>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> GetQuestionsByExam(int examId)
        {
            try
            {
                // Kiểm tra sự tồn tại của bài kiểm tra
                var exam = await _context.Exams
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.ExamId == examId);

                if (exam == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                int numberOfQuestions = exam.RandomMultipleChoiceCount;

                // Nhóm các câu hỏi trắc nghiệm theo dạng câu
                var groupedQuestions = await _context.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Where(q => q.ExamId == examId)
                    .GroupBy(q => q.AnswerGroupNumber)
                    .ToListAsync();

                var selectedQuestions = new List<QuestionPreviewDto>();

                if (groupedQuestions.Any())
                {
                    int questionsPerGroup = numberOfQuestions / groupedQuestions.Count;
                    int remainingQuestions = numberOfQuestions % groupedQuestions.Count;

                    foreach (var group in groupedQuestions)
                    {
                        int takeCount = questionsPerGroup + (remainingQuestions > 0 ? 1 : 0);
                        remainingQuestions--;

                        var randomQuestions = group.OrderBy(q => Guid.NewGuid()).Take(takeCount)
                            .Select(q => new QuestionPreviewDto(
                                q.Id,
                                q.Content,
                                q.Type,
                                q.CreatedAt,
                                q.Choices,
                                q.CorrectAnswerIndex,
                                null
                            ))
                            .ToList();

                        selectedQuestions.AddRange(randomQuestions);
                    }
                }

                // Truy vấn câu hỏi điền khuyết
                var fillInBlankQuestions = await _context.Questions
                    .OfType<FillInBlankQuestion>()
                    .Where(q => q.ExamId == examId)
                    .OrderBy(q => q.CreatedAt)
                    .Select(q => new QuestionPreviewDto(
                        q.Id,
                        q.Content,
                        q.Type,
                        q.CreatedAt,
                        null,
                        null,
                        q.CorrectAnswer
                    ))
                    .ToListAsync();

                // Hợp nhất danh sách và sắp xếp theo CreatedAt
                var questions = selectedQuestions
                    .Concat(fillInBlankQuestions)
                    .OrderBy(q => q.CreatedAt)
                    .ToList();

                if (!questions.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy câu hỏi cho bài kiểm tra này." }));
                }

                return Ok(Result<IEnumerable<QuestionPreviewDto>>.Success(questions));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }


        // Tạo mới bài kiểm tra
        [HttpPost]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]

        public async Task<IActionResult> CreateExam([FromBody] CreateExamRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray();
                return BadRequest(Result<object>.Failure(errors));
            }

            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var courseExists = await _context.Courses
                    .AsNoTracking()
                    .AnyAsync(c => c.CourseId == request.CourseId && c.LecturerId == lecturerId);

                if (!courseExists)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền tạo bài kiểm tra cho khóa học này." }));
                }

                var newExam = new Exam
                {
                    Title = request.Title,
                    Description = request.Description,
                    CourseId = request.CourseId,
                    RandomMultipleChoiceCount = request.RandomMultipleChoiceCount
                };

                await _context.Exams.AddAsync(newExam);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Tạo mới khóa học thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        //Cập nhật bài kiểm tra
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]

        public async Task<IActionResult> UpdateExam(int id, [FromBody] UpdateExamRequest request)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var examInfo = await _context.Exams
                .AsNoTracking()
                .Where(e => e.ExamId == id)
                .Select(e => new
                {
                    e.ExamId,
                    LecturerId = e.Course != null ? e.Course.LecturerId : null
                })
                .FirstOrDefaultAsync();

                if (examInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                if (examInfo.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền chỉnh sửa bài kiểm tra này." }));
                }
                var affectedRows = await _context.Exams
                    .Where(e => e.ExamId == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(e => e.Title, request.Title)
                        .SetProperty(e => e.Description, request.Description)
                        .SetProperty(e => e.RandomMultipleChoiceCount, request.RandomMultipleChoiceCount)
                    );

                if (affectedRows > 0)
                {
                    return Ok(Result<string>.Success("Cập nhật bài kiểm tra thành công!"));
                }
                else
                {
                    return StatusCode(500, Result<object>.Failure(new[] { "Cập nhật bài kiểm tra thất bại." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Xóa bài kiểm tra
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]

        public async Task<IActionResult> DeleteExam(int id)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var examInfo = await _context.Exams
                    .AsNoTracking()
                    .Where(e => e.ExamId == id)
                    .Select(e => new
                    {
                        e.ExamId,
                        CourseLecturerId = e.Course != null ? e.Course.LecturerId : null
                    })
                    .FirstOrDefaultAsync();

                if (examInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                if (examInfo.CourseLecturerId == null || examInfo.CourseLecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xóa bài kiểm tra này." }));
                }

                var affectedRows = await _context.Exams
                    .Where(e => e.ExamId == id)
                    .ExecuteDeleteAsync();

                if (affectedRows > 0)
                {
                    return Ok(Result<string>.Success("Xóa bài kiểm tra thành công!"));
                }
                else
                {
                    return StatusCode(500, Result<object>.Failure(new[] { "Xóa bài kiểm tra thất bại." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        public record CreateExamRequest(int CourseId, string Title, string? Description, int RandomMultipleChoiceCount);
        public record UpdateExamRequest(string Title, string? Description, int RandomMultipleChoiceCount);



        // Người học nộp kết quả bài kiểm tra
        [HttpPost("{examId}/submit")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        public async Task<IActionResult> SubmitExam(int examId, [FromBody] SubmitExamRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                                       .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                                       .ToArray();
                return BadRequest(Result<object>.Failure(errors));
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin người học không hợp lệ." }));
            }

            try
            {
                var exam = await _context.Exams.FindAsync(examId);
                if (exam == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }
                var hasSubmitted = await _context.ExamResults
                    .AnyAsync(er => er.ExamId == examId && er.StudentId == studentId);
                if (hasSubmitted)
                {
                    return BadRequest(Result<object>.Failure(new[] { "Bạn đã nộp bài kiểm tra này rồi." }));
                }
                var examResult = new ExamResult
                {
                    StudentId = studentId,
                    ExamId = examId,
                    Score = request.Score,
                    SubmissionTime = DateTime.UtcNow
                };

                await _context.ExamResults.AddAsync(examResult);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Nộp bài kiểm tra thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        // Người học xem kết quả bài kiểm tra của mình
        [HttpGet("{examId}/result")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetExamResult(int examId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin người học không hợp lệ." }));
            }

            try
            {
                var examResult = await _context.ExamResults
                    .AsNoTracking()
                    .Where(er => er.ExamId == examId && er.StudentId == studentId)
                    .Select(er => new
                    {
                        er.ResultId,
                        er.Score,
                        er.SubmissionTime
                    })
                    .FirstOrDefaultAsync();

                if (examResult == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Bạn chưa có kết quả cho bài kiểm tra này." }));
                }

                return Ok(Result<object>.Success(examResult));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        // Giảng viên xem danh sách kết quả của học viên
        [HttpGet("{examId}/results")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<IEnumerable<object>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        public async Task<IActionResult> GetExamResults(int examId)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var examInfo = await _context.Exams
                .AsNoTracking()
                .Where(e => e.ExamId == examId)
                .Select(e => new { e.ExamId, LecturerId = e.Course.LecturerId })
                .FirstOrDefaultAsync();

                if (examInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                if (examInfo.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xem kết quả bài kiểm tra này." }));
                }

                // Truy vấn kết quả bài kiểm tra của tất cả sinh viên dựa trên examId
                var results = await _context.ExamResults
                    .AsNoTracking()
                    .Where(er => er.ExamId == examId)
                    .Select(er => new
                    {
                        er.ResultId,
                        StudentName = er.Student.FullName,
                        er.Score,
                        er.SubmissionTime
                    })
                    .ToListAsync();

                return Ok(Result<IEnumerable<object>>.Success(results));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        public record SubmitExamRequest(float Score);
    }
}
