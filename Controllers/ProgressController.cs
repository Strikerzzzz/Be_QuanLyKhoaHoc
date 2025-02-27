using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleProject.Common;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProgressController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProgressController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("progress")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<ProgressPagedResult>), 200)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetProgressForCourse([FromQuery] int courseId, [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10, [FromQuery] string search = null)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                var query = _context.Progresses.AsNoTracking()
                    .Where(p => p.CourseId == courseId &&
                                (string.IsNullOrEmpty(search) ||
                                 EF.Functions.Like(p.StudentId, $"%{search}%") ||
                                 EF.Functions.Like(p.Student.FullName, $"%{search}%") ||
                                 EF.Functions.Like(p.Student.Email, $"%{search}%") ||
                                 EF.Functions.Like(p.Student.UserName, $"%{search}%")));

                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không có tiến độ nào cho khóa học này." }));
                }

                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                if (page > totalPages) page = totalPages;

                var progressList = await query
                    .OrderBy(p => p.StudentId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProgressDto(
                        p.ProgressId,
                        p.StudentId,
                        p.Student.FullName,
                        p.CompletionRate,
                        p.IsCompleted,
                        p.UpdatedAt
                    ))
                    .ToListAsync();

                if (!progressList.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { "Không có dữ liệu tiến độ nào phù hợp." }));
                }

                var result = new ProgressPagedResult(progressList, totalCount);

                return Ok(Result<ProgressPagedResult>.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        [HttpGet("my-progress")]
        [ProducesResponseType(typeof(Result<ProgressDto>), 200)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        public async Task<IActionResult> GetStudentProgressInCourse([FromQuery] int courseId)
        {
            try
            {
                var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(studentId))
                {
                    return Unauthorized(Result<object>.Failure(new[] { "Không xác định được học viên." }));
                }

                var progress = await _context.Progresses.AsNoTracking()
                    .Where(p => p.CourseId == courseId && p.StudentId == studentId)
                    .Select(p => new ProgressDto(
                        p.ProgressId,
                        p.StudentId,
                        p.Student.FullName,
                        p.CompletionRate,
                        p.IsCompleted,
                        p.UpdatedAt
                    ))
                    .FirstOrDefaultAsync();

                if (progress == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy tiến độ học tập cho khóa học này." }));
                }

                return Ok(Result<ProgressDto>.Success(progress));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        [HttpPost("create")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> CreateStudentProgress([FromBody] CreateProgressRequest request)
        {
            try
            {
                var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(studentId))
                {
                    return Unauthorized(Result<object>.Failure(new[] { "Không xác định được học viên." }));
                }

                var existingProgress = await _context.Progresses
                    .AnyAsync(p => p.CourseId == request.CourseId && p.StudentId == studentId);

                if (existingProgress)
                {
                    return BadRequest(Result<object>.Failure(new[] { "Tiến độ cho khóa học này đã tồn tại." }));
                }

                var newProgress = new Identity.Entities.Progress
                {
                    StudentId = studentId,
                    CourseId = request.CourseId,
                    CompletionRate = 0,
                    IsCompleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Progresses.Add(newProgress);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Thêm tiến độ thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }

        }
        [HttpPut("update-lesson-progress")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> UpdateCompletionRate(int courseId, int lessonId)
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (studentId == null)
                return Unauthorized(Result<object>.Failure(new[] { "Không xác định được học viên." }));

            // Kiểm tra bài học có thuộc khóa học không
            bool lessonExists = await _context.Lessons
                .AnyAsync(l => l.LessonId == lessonId && l.CourseId == courseId);
            if (!lessonExists)
                return BadRequest(Result<object>.Failure(new[] { "Bài học không tồn tại trong khóa học." }));

            // Kiểm tra xem học viên đã hoàn thành bài học này chưa
            bool alreadyCompleted = await _context.CompletedLessons
                .AnyAsync(cl => cl.StudentId == studentId && cl.LessonId == lessonId);

            if (!alreadyCompleted)
            {
                var completedLesson = new CompletedLesson
                {
                    StudentId = studentId,
                    LessonId = lessonId,
                    CompletedAt = DateTime.UtcNow
                };

                _context.CompletedLessons.Add(completedLesson);
                await _context.SaveChangesAsync();
            }

            // Tìm tiến độ học tập của học viên
            var progress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.CourseId == courseId && p.StudentId == studentId);

            if (progress == null)
                return NotFound(Result<object>.Failure(new[] { "Không tìm thấy tiến độ học tập." }));

            var totalLessons = await _context.Lessons.CountAsync(l => l.CourseId == courseId);
            if (totalLessons == 0)
                return BadRequest(Result<object>.Failure(new[] { "Khóa học không có bài giảng nào." }));

            var completedLessons = await _context.CompletedLessons
                .Where(cl => cl.StudentId == studentId && _context.Lessons.Any(l => l.LessonId == cl.LessonId && l.CourseId == courseId))
                .CountAsync();

            progress.CompletionRate = (float)completedLessons / totalLessons * 100;
            progress.IsCompleted = (completedLessons == totalLessons);
            progress.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(Result<string>.Success("Cập nhật tiến độ thành công."));
        }

        [HttpPost("add-assignment-result")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> AddAssignmentResult(int lessonId, int assignmentId, float score)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (studentId == null)
                return Unauthorized(Result<object>.Failure(new[] { "Không xác định được học viên." }));

            if (score < 0 || score > 100)
                return BadRequest(Result<object>.Failure(new[] { "Điểm không hợp lệ." }));

            // Kiểm tra bài tập có thuộc bài học không
            var assignment = await _context.Assignments
                .Include(a => a.Lesson) // Lấy luôn Lesson để xác định CourseId
                .FirstOrDefaultAsync(a => a.AssignmentId == assignmentId && a.LessonId == lessonId);

            if (assignment == null)
                return BadRequest(Result<object>.Failure(new[] { "Bài tập không tồn tại trong bài học." }));

            // Kiểm tra xem đã có điểm chưa
            var existingResult = await _context.AssignmentResults
                .FirstOrDefaultAsync(ar => ar.StudentId == studentId && ar.AssignmentId == assignmentId);

            if (existingResult == null)
            {
                // Chưa có điểm, thêm mới
                var newResult = new AssignmentResult
                {
                    StudentId = studentId,
                    AssignmentId = assignmentId,
                    Score = score,
                    SubmissionTime = DateTime.UtcNow
                };
                _context.AssignmentResults.Add(newResult);
            }
            else if (existingResult.Score < score)
            {
                // Ghi đè điểm mới nếu cao hơn
                existingResult.Score = score;
                existingResult.SubmissionTime = DateTime.UtcNow;
            }
            else
            {
                return Ok(Result<string>.Success("Điểm không thay đổi do điểm cũ cao hơn hoặc bằng."));
            }

            await _context.SaveChangesAsync();

            return Ok(Result<string>.Success("Thêm điểm bài tập thành công."));
        }

        [HttpPost("add-exam-result")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> AddExamResult(int courseId, int examId, float score)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (studentId == null)
                return Unauthorized(Result<object>.Failure(new[] { "Không xác định được học viên." }));

            if (score < 0 || score > 100)
                return BadRequest(Result<object>.Failure(new[] { "Điểm không hợp lệ." }));

            // Kiểm tra bài thi có thuộc khóa học không
            var exam = await _context.Exams
                .FirstOrDefaultAsync(e => e.ExamId == examId && e.CourseId == courseId);

            if (exam == null)
                return BadRequest(Result<object>.Failure(new[] { "Bài thi không tồn tại trong khóa học." }));

            // Kiểm tra xem đã có điểm chưa
            var existingResult = await _context.ExamResults
                .FirstOrDefaultAsync(er => er.StudentId == studentId && er.ExamId == examId);

            if (existingResult == null)
            {
                // Chưa có điểm, thêm mới
                var newResult = new ExamResult
                {
                    StudentId = studentId,
                    ExamId = examId,
                    Score = score,
                    SubmissionTime = DateTime.UtcNow
                };
                _context.ExamResults.Add(newResult);
            }
            else if (existingResult.Score < score)
            {
                // Ghi đè điểm mới nếu cao hơn
                existingResult.Score = score;
                existingResult.SubmissionTime = DateTime.UtcNow;
            }
            else
            {
                return Ok(Result<string>.Success("Điểm không thay đổi do điểm cũ cao hơn hoặc bằng."));
            }

            await _context.SaveChangesAsync();

            return Ok(Result<string>.Success("Thêm điểm bài thi thành công."));
        }

        [HttpDelete("delete/{studentId}/{courseId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> DeleteProgress(string studentId, int courseId)
        {
            if (string.IsNullOrWhiteSpace(studentId))
                return BadRequest(Result<object>.Failure(new[] { "Mã học viên không hợp lệ." }));

            // Tìm tiến độ của học viên trong khóa học
            var progress = await _context.Progresses
                .FirstOrDefaultAsync(p => p.StudentId == studentId && p.CourseId == courseId);

            if (progress == null)
                return NotFound(Result<object>.Failure(new[] { "Không tìm thấy tiến độ học viên trong khóa học." }));

            // Lấy danh sách bài tập trong khóa học
            var assignmentIds = await _context.Assignments
                .Where(a => a.Lesson.CourseId == courseId)
                .Select(a => a.AssignmentId)
                .ToListAsync();

            // Lấy danh sách bài thi trong khóa học
            var examIds = await _context.Exams
                .Where(e => e.CourseId == courseId)
                .Select(e => e.ExamId)
                .ToListAsync();

            var assignmentResults = await _context.AssignmentResults
                .Where(ar => ar.StudentId == studentId && assignmentIds.Contains(ar.AssignmentId ?? 0))
                .ToListAsync();

            _context.AssignmentResults.RemoveRange(assignmentResults);

            // Xóa kết quả bài thi của học viên trong khóa học
            var examResults = await _context.ExamResults
                .Where(er => er.StudentId == studentId && examIds.Contains(er.ExamId ?? 0))
                .ToListAsync();
            _context.ExamResults.RemoveRange(examResults);

            // Xóa tiến độ học viên
            _context.Progresses.Remove(progress);

            await _context.SaveChangesAsync();

            return Ok(Result<string>.Success("Xóa tiến độ học viên và các kết quả liên quan thành công."));
        }


    }
    public record CreateProgressRequest([Required] int CourseId);

    public record ProgressDto(
        int ProgressId,
        string StudentId,
        string StudentName,
        float CompletionRate,
        bool IsCompleted,
        DateTime UpdatedAt
    );
    public record ProgressPagedResult(IEnumerable<ProgressDto> Progresses, int TotalCount);


}
