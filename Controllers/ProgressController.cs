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
                .Join(_context.Lessons,
                      cl => cl.LessonId,
                      l => l.LessonId,
                      (cl, l) => new { cl, l })
                .Where(x => x.cl.StudentId == studentId && x.l.CourseId == courseId)
                .CountAsync();


            progress.CompletionRate = (float)completedLessons / totalLessons * 100;
            progress.IsCompleted = (completedLessons == totalLessons);
            progress.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(Result<string>.Success("Cập nhật tiến độ thành công."));
        }


        [HttpDelete("delete/{studentId}/{courseId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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

        [HttpGet("lesson-learn/{courseId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<List<LessonLearnDto>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        public async Task<IActionResult> GetLessonsByCourseForLearn(int courseId)
        {
            try
            {
                // Lấy userId từ token JWT
                var userId= User.FindFirstValue(ClaimTypes.NameIdentifier);
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

                // Lấy danh sách bài học của khóa học và kiểm tra trạng thái hoàn thành
                var lessons = await _context.Lessons
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.LessonId)
                    .Select(l => new LessonLearnDto(
                        l.LessonId,
                        l.Title,
                        _context.CompletedLessons.Any(cl => cl.LessonId == l.LessonId && cl.StudentId == userId)
                    ))
                    .ToListAsync();

                // Kiểm tra xem có bài học nào không
                if (lessons.Count == 0)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không có bài học nào trong khóa học này." }));
                }

                // Trả về danh sách bài học
                return Ok(Result<List<LessonLearnDto>>.Success(lessons));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
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

    public record LessonLearnDto(
             int LessonId,
             string Title,
            bool Completed
         );
    public record ProgressPagedResult(IEnumerable<ProgressDto> Progresses, int TotalCount);


}
