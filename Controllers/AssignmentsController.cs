using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Be_QuanLyKhoaHoc.Identity.Entities;
using SampleProject.Common;
using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Enums;
using System.Linq;
using Be_QuanLyKhoaHoc.DTO;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("lesson/{lessonId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> GetAssignmentByLesson(int lessonId)
        {
            try
            {
                // Kiểm tra sự tồn tại của bài học
                var lesson = await _context.Lessons
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.LessonId == lessonId);

                if (lesson == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài học." }));
                }

                // Lấy bài tập của bài học (quan hệ 1-1)
                var assignment = await _context.Assignments
                    .AsNoTracking()
                    .Where(a => a.LessonId == lessonId)
                    .Select(a => new
                    {
                        a.AssignmentId,
                        a.Title,
                        a.Description,
                        a.RandomMultipleChoiceCount
                    })
                    .SingleOrDefaultAsync();

                if (assignment == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập cho bài học này." }));
                }

                return Ok(Result<object>.Success(assignment));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        [HttpGet("{assignmentId}/questions")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<IEnumerable<QuestionPreviewDto>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> GetQuestionsByAssignment(int assignmentId)
        {
            try
            {
                var assignment = await _context.Assignments
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);

                if (assignment == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                int numberOfQuestions = assignment.RandomMultipleChoiceCount;

                var groupedQuestions = await _context.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Where(q => q.AssignmentId == assignmentId)
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

                var fillInBlankQuestions = await _context.Questions
                    .OfType<FillInBlankQuestion>()
                    .Where(q => q.AssignmentId == assignmentId)
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

                var questions = selectedQuestions
                    .Concat(fillInBlankQuestions)
                    .OrderBy(q => q.CreatedAt)
                    .ToList();

                if (!questions.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy câu hỏi cho bài tập này." }));
                }

                return Ok(Result<IEnumerable<QuestionPreviewDto>>.Success(questions));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }


        // Tạo mới bài tập
        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
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
                var lessonInfo = await _context.Lessons
                    .AsNoTracking()
                    .Where(l => l.LessonId == request.LessonId)
                    .Select(l => new
                    {
                        l.LessonId,
                        CourseLecturerId = l.Course != null ? l.Course.LecturerId : null
                    })
                    .FirstOrDefaultAsync();

                if (lessonInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài học." }));
                }

                if (lessonInfo.CourseLecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền tạo bài tập cho bài học này." }));
                }

                var newAssignment = new Assignment
                {
                    Title = request.Title,
                    Description = request.Description,
                    LessonId = request.LessonId,
                    RandomMultipleChoiceCount = request.RandomMultipleChoiceCount
                };

                await _context.Assignments.AddAsync(newAssignment);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Tạo mới bài tập thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Cập nhật bài tập
        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> UpdateAssignment(int id, [FromBody] UpdateAssignmentRequest request)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var assignmentInfo = await _context.Assignments
                   .AsNoTracking()
                   .Where(a => a.AssignmentId == id)
                   .Select(a => new
                   {
                       a.AssignmentId,
                       LecturerId = a.Lesson != null && a.Lesson.Course != null ? a.Lesson.Course.LecturerId : null
                   })
                   .FirstOrDefaultAsync();

                if (assignmentInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                if (assignmentInfo.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền chỉnh sửa bài tập này." }));
                }

                var affectedRows = await _context.Assignments
                    .Where(a => a.AssignmentId == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(a => a.Title, request.Title)
                        .SetProperty(a => a.Description, request.Description)
                        .SetProperty(a => a.RandomMultipleChoiceCount, request.RandomMultipleChoiceCount)
                    );

                if (affectedRows > 0)
                {
                    return Ok(Result<string>.Success("Cập nhật bài tập thành công!"));
                }
                else
                {
                    return StatusCode(500, Result<object>.Failure(new[] { "Cập nhật bài tập thất bại." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Xóa bài tập
        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var assignmentInfo = await _context.Assignments
                    .AsNoTracking()
                    .Where(a => a.AssignmentId == id)
                    .Select(a => new
                    {
                        a.AssignmentId,
                        LecturerId = a.Lesson != null && a.Lesson.Course != null ? a.Lesson.Course.LecturerId : null
                    })
                    .FirstOrDefaultAsync();

                if (assignmentInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                if (assignmentInfo.LecturerId == null || assignmentInfo.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xóa bài tập này." }));
                }

                var affectedRows = await _context.Assignments
                    .Where(a => a.AssignmentId == id)
                    .ExecuteDeleteAsync();

                if (affectedRows > 0)
                {
                    return Ok(Result<string>.Success("Xóa bài tập thành công!"));
                }
                else
                {
                    return StatusCode(500, Result<object>.Failure(new[] { "Xóa bài tập thất bại." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }



        [HttpPost("{assignmentId}/submit")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, [FromBody] SubmitAssignmentRequest request)
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
                // Truy vấn đồng thời cả Assignment và điểm đã nộp trước đó (nếu có)
                var assignmentData = await _context.Assignments
                    .Where(a => a.AssignmentId == assignmentId)
                    .Select(a => new
                    {
                        Assignment = a,
                        ExistingResult = _context.AssignmentResults
                            .FirstOrDefault(ar => ar.AssignmentId == assignmentId && ar.StudentId == studentId)
                    })
                    .FirstOrDefaultAsync();

                if (assignmentData?.Assignment == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                var existingResult = assignmentData.ExistingResult;

                if (existingResult == null)
                {
                    // Chưa nộp, thêm mới
                    var newResult = new AssignmentResult
                    {
                        StudentId = studentId,
                        AssignmentId = assignmentId,
                        Score = request.Score,
                        SubmissionTime = DateTime.UtcNow
                    };

                    await _context.AssignmentResults.AddAsync(newResult);
                    await _context.SaveChangesAsync();
                    return Ok(Result<string>.Success("Nộp bài tập thành công!"));
                }
                else if (request.Score > existingResult.Score)
                {
                    // Cập nhật nếu điểm mới cao hơn
                    existingResult.Score = request.Score;
                    existingResult.SubmissionTime = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    return Ok(Result<string>.Success("Cập nhật điểm thành công!"));
                }

                return Ok(Result<string>.Success("Điểm không thay đổi do điểm cũ cao hơn hoặc bằng."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        [HttpGet("learning-progress/{courseId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<List<LessonLearnDto>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetLearningProgress(int courseId)
        {
            try
            {
                // Lấy userId từ token JWT
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(Result<object>.Failure(new[] { "Không thể xác định người dùng." }));
                }

                // Kiểm tra xem khóa học có tồn tại không
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                // Lấy danh sách bài học của khóa học kèm theo trạng thái hoàn thành và điểm bài tập (nếu có)
                var lessons = await _context.Lessons
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.LessonId)
                    .Select(l => new
                    {
                        l.LessonId,
                        l.Title,
                        IsCompleted = _context.CompletedLessons
                            .Any(cl => cl.LessonId == l.LessonId && cl.StudentId == userId),
                        AssignmentScore = l.Assignments != null
                            ? _context.AssignmentResults
                                .Where(ar => ar.AssignmentId == l.Assignments.AssignmentId && ar.StudentId == userId)
                                .Select(ar => (float?)ar.Score)
                                .FirstOrDefault()
                            : null
                    })
                    .ToListAsync();


                // Kiểm tra xem có bài học nào không
                if (!lessons.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { "Không có bài học nào trong khóa học này." }));
                }

                return Ok(Result<object>.Success(lessons));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        [HttpGet("{assignmentId}/results")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<IEnumerable<object>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        public async Task<IActionResult> GetAssignmentResults(int assignmentId)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                // Kiểm tra sự tồn tại của bài tập và xác thực quyền của giảng viên thông qua quan hệ (Assignment → Lesson → Course)
                var assignmentInfo = await _context.Assignments
                    .AsNoTracking()
                    .Where(a => a.AssignmentId == assignmentId)
                    .Select(a => new
                    {
                        a.AssignmentId,
                        LecturerId = a.Lesson.Course.LecturerId
                    })
                    .FirstOrDefaultAsync();

                if (assignmentInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                if (assignmentInfo.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xem bài nộp của bài tập này." }));
                }

                // Truy vấn danh sách kết quả nộp bài tập của học viên
                var results = await _context.AssignmentResults
                    .AsNoTracking()
                    .Where(ar => ar.AssignmentId == assignmentId)
                    .Select(ar => new
                    {
                        ar.ResultId,
                        StudentName = ar.Student.FullName,
                        ar.Score,
                        ar.SubmissionTime
                    })
                    .ToListAsync();

                return Ok(Result<IEnumerable<object>>.Success(results));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }
        [HttpGet("{assignmentId}/score-line-chart")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<IEnumerable<LineChartDto>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        public async Task<IActionResult> GetScoreLineChart(int assignmentId)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                // Kiểm tra sự tồn tại của bài tập và xác thực quyền của giảng viên thông qua quan hệ (Assignment → Lesson → Course)
                var assignmentInfo = await _context.Assignments
                    .AsNoTracking()
                    .Where(a => a.AssignmentId == assignmentId)
                    .Select(a => new
                    {
                        a.AssignmentId,
                        LecturerId = a.Lesson.Course.LecturerId
                    })
                    .FirstOrDefaultAsync();

                if (assignmentInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                if (assignmentInfo.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xem bài nộp của bài tập này." }));
                }

                // Truy vấn danh sách kết quả nộp bài tập của học viên
                var results = await _context.AssignmentResults
                    .AsNoTracking()
                    .Where(ar => ar.AssignmentId == assignmentId)
                    .Select(ar => new
                    {
                        ar.ResultId,
                        ar.Score
                    })
                    .ToListAsync();

                // Tính số lượng sinh viên ở mỗi mức điểm từ 1-10, 11-20,..., 91-100
                var scoreRanges = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

                // Tạo danh sách các điểm và số lượng sinh viên trong mỗi mức điểm
                var lineChartData = scoreRanges.Select(range => new LineChartDto
                {
                    Title = $"{range - 9}-{range}", // Tiêu đề cho từng mức điểm
                    Value = results.Count(r => r.Score >= range - 9 && r.Score <= range)
                }).ToList();

                return Ok(Result<IEnumerable<LineChartDto>>.Success(lineChartData));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        public class LineChartDto
        {
            public string Title { get; set; }  // Tiêu đề (Mức điểm, ví dụ: "1-10", "11-20", ...)
            public int Value { get; set; }  // Số lượng sinh viên đạt mức điểm này
        }

        public record CreateAssignmentRequest(int LessonId, string Title, string? Description, int RandomMultipleChoiceCount);
        public record UpdateAssignmentRequest(string Title, string? Description, int RandomMultipleChoiceCount);

        public record SubmitAssignmentRequest(float Score);
    }
}
