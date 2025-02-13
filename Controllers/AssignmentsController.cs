using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Be_QuanLyKhoaHoc.Identity.Entities;
using SampleProject.Common;
using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Enums;
using System.Linq;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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
                        a.Title
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
        [ProducesResponseType(typeof(Result<IEnumerable<object>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> GetQuestionsByAssignment(int assignmentId)
        {
            try
            {
                var assignmentExists = await _context.Assignments
                    .AsNoTracking()
                    .AnyAsync(a => a.AssignmentId == assignmentId);
                if (!assignmentExists)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                // Truy vấn cho MultipleChoiceQuestion
                var multipleChoiceQuestions = await _context.Questions
                    .OfType<MultipleChoiceQuestion>()
                    .Where(q => q.AssignmentId == assignmentId)
                    .OrderBy(q => q.CreatedAt)
                    .Select(q => new
                    {
                        q.Id,
                        q.Content,
                        q.Type,
                        q.CreatedAt,
                        Choices = q.Choices,
                        CorrectAnswerIndex = (int?)q.CorrectAnswerIndex, // ép kiểu về int?
                        CorrectAnswer = (string)null
                    })
                    .ToListAsync();

                // Truy vấn cho FillInBlankQuestion
                var fillInBlankQuestions = await _context.Questions
                    .OfType<FillInBlankQuestion>()
                    .Where(q => q.AssignmentId == assignmentId)
                    .OrderBy(q => q.CreatedAt)
                    .Select(q => new
                    {
                        q.Id,
                        q.Content,
                        q.Type,
                        q.CreatedAt,
                        Choices = (string)null,
                        CorrectAnswerIndex = (int?)null,
                        CorrectAnswer = q.CorrectAnswer
                    })
                    .ToListAsync();

                // Hợp nhất danh sách và sắp xếp theo CreatedAt
                var questions = multipleChoiceQuestions
                    .Concat(fillInBlankQuestions)
                    .OrderBy(q => q.CreatedAt)
                    .ToList();

                if (!questions.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy câu hỏi cho bài tập này." }));
                }

                return Ok(Result<IEnumerable<object>>.Success(questions));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Tạo mới bài tập
        [HttpPost]
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
                    LessonId = request.LessonId
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
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> SubmitAssignment(int assignmentId, [FromBody] SubmitAssignmentRequest request)
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
                var assignment = await _context.Assignments
                                               .AsNoTracking()
                                               .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId);
                if (assignment == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }
                var hasSubmitted = await _context.AssignmentResults
                    .AnyAsync(ar => ar.AssignmentId == assignmentId && ar.StudentId == studentId);
                if (hasSubmitted)
                {
                    return BadRequest(Result<object>.Failure(new[] { "Bạn đã nộp bài tập này rồi." }));
                }

                var assignmentResult = new AssignmentResult
                {
                    StudentId = studentId,
                    AssignmentId = assignmentId,
                    Score = request.Score,
                    SubmissionTime = DateTime.UtcNow
                };

                await _context.AssignmentResults.AddAsync(assignmentResult);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Nộp bài tập thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        [HttpGet("{assignmentId}/result")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetAssignmentResult(int assignmentId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin người học không hợp lệ." }));
            }

            try
            {
                var result = await _context.AssignmentResults
                    .AsNoTracking()
                    .Where(ar => ar.AssignmentId == assignmentId && ar.StudentId == studentId)
                    .Select(ar => new
                    {
                        ar.ResultId,
                        ar.Score,
                        ar.SubmissionTime
                    })
                    .FirstOrDefaultAsync();

                if (result == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Bạn chưa có kết quả cho bài tập này." }));
                }

                return Ok(Result<object>.Success(result));
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


        public record CreateAssignmentRequest(int LessonId, string Title, string? Description);
        public record UpdateAssignmentRequest(string Title, string? Description);

        public record SubmitAssignmentRequest(float Score);
    }
}
