using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Be_QuanLyKhoaHoc.Identity.Entities;
using SampleProject.Common;
using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Enums;
using Be_QuanLyKhoaHoc.DTO;
using static Be_QuanLyKhoaHoc.Controllers.AssignmentsController;
using System.Linq;
using Be_QuanLyKhoaHoc.Services;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DeleteService _deleteService;

        public ExamsController(ApplicationDbContext context, DeleteService deleteService)
        {
            _context = context;
            _deleteService = deleteService;
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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

            var result = await _deleteService.DeleteExamAsync(id, lecturerId);

            if (!result.Succeeded)
            {
                var message = result.Errors?.FirstOrDefault() ?? "Đã xảy ra lỗi.";

                if (message.Contains("không tìm thấy", StringComparison.OrdinalIgnoreCase))
                    return NotFound(Result<object>.Failure(new[] { message }));

                if (message.Contains("không có quyền", StringComparison.OrdinalIgnoreCase))
                    return StatusCode(403, Result<object>.Failure(new[] { message }));

                return StatusCode(500, Result<object>.Failure(new[] { message }));
            }

            return Ok(result);
        }


        public record CreateExamRequest(int CourseId, string Title, string? Description, int RandomMultipleChoiceCount);
        public record UpdateExamRequest(string Title, string? Description, int RandomMultipleChoiceCount);



        [HttpPost("{examId}/submit")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        public async Task<IActionResult> SubmitExam(int examId, [FromBody] SubmitExamRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray();
                return BadRequest(Result<object>.Failure(errors));
            }

            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin người học không hợp lệ." }));
            }

            if (request.Score < 0 || request.Score > 100)
            {
                return BadRequest(Result<object>.Failure(new[] { "Điểm không hợp lệ (phải từ 0 đến 100)." }));
            }

            try
            {
                var examData = await _context.Exams
                    .Where(e => e.ExamId == examId)
                    .Select(e => new
                    {
                        Exam = e,
                        ExistingResult = _context.ExamResults
                            .FirstOrDefault(er => er.ExamId == examId && er.StudentId == studentId)
                    })
                    .FirstOrDefaultAsync();

                if (examData?.Exam == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                var existingResult = examData.ExistingResult;

                if (existingResult == null)
                {
                    // Nếu chưa nộp bài, thêm mới
                    var newResult = new ExamResult
                    {
                        StudentId = studentId,
                        ExamId = examId,
                        Score = request.Score,
                        SubmissionTime = DateTime.UtcNow
                    };

                    await _context.ExamResults.AddAsync(newResult);
                    await _context.SaveChangesAsync();
                    return Ok(Result<string>.Success("Nộp bài kiểm tra thành công!"));
                }
                else if (request.Score > existingResult.Score)
                {
                    // Cập nhật nếu điểm mới cao hơn
                    existingResult.Score = request.Score;
                    existingResult.SubmissionTime = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    return Ok(Result<string>.Success("Cập nhật điểm bài kiểm tra thành công!"));
                }

                return Ok(Result<string>.Success("Điểm không thay đổi do điểm cũ cao hơn hoặc bằng."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }


        // Người học xem kết quả bài kiểm tra của mình
        [HttpGet("{courseId}/result")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetExamResult(int courseId)
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
                    .Where(er => er.Exam != null && er.Exam.CourseId == courseId && er.StudentId == studentId)
                    .Select(er => new
                    {
                        er.ResultId,
                        er.Exam.Title,
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

        [HttpGet("{examId}/results")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<ExamResultPagedResult>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        public async Task<IActionResult> GetExamResults(
        int examId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string studentName = null,
        [FromQuery] bool sortByScore = false)
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

                var query = _context.ExamResults
                 .AsNoTracking()
                 .Where(er => er.ExamId == examId)
                 .Join(_context.Users,
                     er => er.StudentId,
                     s => s.Id,
                     (er, s) => new { er, s });

                // Tìm kiếm theo LIKE
                if (!string.IsNullOrEmpty(studentName))
                {
                    query = query.Where(joined =>
                        EF.Functions.Like(joined.s.FullName, $"%{studentName}%") ||
                        EF.Functions.Like(joined.s.UserName, $"%{studentName}%") ||
                        EF.Functions.Like(joined.s.Email, $"%{studentName}%"));
                }

                var totalCount = await query.CountAsync();
                if (totalCount == 0)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không có kết quả phù hợp." }));
                }

                // Tính tổng số trang
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                if (page > totalPages) page = totalPages;

                // Sắp xếp theo điểm nếu sortByScore = true, còn lại thì sắp xếp tăng dần
                if (sortByScore)
                {
                    query = query.OrderByDescending(joined => joined.er.Score)
                                 .ThenBy(joined => joined.er.SubmissionTime);
                }
                else
                {
                    query = query.OrderBy(joined => joined.er.Score)
                                 .ThenBy(joined => joined.er.SubmissionTime);
                }


                var results = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(joined => new ExamResultDto(
                        joined.er.ResultId,
                        joined.s.FullName,
                        joined.s.UserName,
                        joined.s.Email,
                        joined.er.Score,
                        joined.er.SubmissionTime))
                    .ToListAsync();

                var result = new ExamResultPagedResult(results, totalCount);
                return Ok(Result<ExamResultPagedResult>.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        [HttpGet("chart/{courseId}/score-line-exam")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<IEnumerable<LineChartDto>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetCourseExamScoreLineChart(int courseId)
        {
            // Lấy ID giảng viên từ token
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                // Kiểm tra khóa học và quyền truy cập
                var courseInfo = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.CourseId == courseId)
                    .Select(c => new { c.CourseId, c.LecturerId })
                    .FirstOrDefaultAsync();

                if (courseInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                if (courseInfo.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xem kết quả của khóa học này." }));
                }

                // Lấy danh sách bài kiểm tra của khóa học
                var examIds = await _context.Exams
                    .Where(e => e.CourseId == courseId)
                    .Select(e => e.ExamId)
                    .ToListAsync();

                if (!examIds.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { "Không có bài kiểm tra nào trong khóa học này." }));
                }

                // Lấy kết quả điểm của học viên
                var results = await _context.ExamResults
                    .AsNoTracking()
                    .Where(er => examIds.Contains(er.ExamId ?? 0))
                    .Select(er => new { er.ResultId, er.Score })
                    .ToListAsync();

                if (!results.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { "Không có kết quả bài kiểm tra nào trong khóa học này." }));
                }

                // Định nghĩa các khoảng điểm
                var scoreRanges = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

                // Tạo dữ liệu biểu đồ
                var lineChartData = scoreRanges.Select(range => new LineChartDto
                {
                    Title = $"{range - 9}-{range}", // Ví dụ: "1-10", "11-20", ...
                    Value = results.Count(r => r.Score >= range - 9 && r.Score <= range)
                }).ToList();

                return Ok(Result<IEnumerable<LineChartDto>>.Success(lineChartData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }
        public record SubmitExamRequest(float Score);
        public record ExamResultDto(
        int ResultId,
        string? StudentName,
        string? UserName,
        string? Email,
        double Score,
        DateTime? SubmissionTime);

        public record ExamResultPagedResult(
        IEnumerable<ExamResultDto> Results,
        int TotalCount);

    }
}
