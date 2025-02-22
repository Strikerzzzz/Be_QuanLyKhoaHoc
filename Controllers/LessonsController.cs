using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Be_QuanLyKhoaHoc.Identity.Entities;
using SampleProject.Common;
using Be_QuanLyKhoaHoc.Identity;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
    public class LessonsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LessonsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("course/{courseId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<LessonPagedResult>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        public async Task<IActionResult> GetLessonsByCourse(int courseId, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                var totalItems = await _context.Lessons.Where(l => l.CourseId == courseId).CountAsync();
                if (totalItems == 0)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không có bài học nào trong khóa học này." }));
                }

                // Tính tổng số trang
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
                if (pageIndex > totalPages) pageIndex = totalPages;
                var lessons = await _context.Lessons
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.LessonId)
                    .Skip((pageIndex - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new LessonDto(
                        l.LessonId,
                        l.Title
                    ))
                    .ToListAsync();

                var result = new LessonPagedResult(lessons, totalItems);

                return Ok(Result<LessonPagedResult>.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }


        [HttpPost]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request)
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
                var course = await _context.Courses.FindAsync(request.CourseId);
                if (course == null || course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền thêm bài học vào khóa học này." }));
                }

                var newLesson = new Lesson
                {
                    Title = request.Title,
                    CourseId = request.CourseId
                };

                await _context.Lessons.AddAsync(newLesson);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Thêm bài học thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Cập nhật bài học
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> UpdateLesson(int id, [FromBody] UpdateLessonRequest request)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var affectedRows = await _context.Lessons
                    .Where(l => l.LessonId == id && l.Course.LecturerId == lecturerId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(l => l.Title, request.Title)
                    );

                if (affectedRows == 0)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài học." }));
                }

                return Ok(Result<string>.Success("Cập nhật bài học thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Xóa bài học
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var lessonInfo = await _context.Lessons
                    .AsNoTracking()
                    .Where(l => l.LessonId == id)
                    .Select(l => new
                    {
                        l.LessonId,
                        LecturerId = l.Course != null ? l.Course.LecturerId : null
                    })
                    .FirstOrDefaultAsync();

                if (lessonInfo == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài học." }));
                }
                if (lessonInfo.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xóa bài học này." }));
                }

                var affectedRows = await _context.Lessons
                    .Where(l => l.LessonId == id)
                    .ExecuteDeleteAsync();

                if (affectedRows > 0)
                {
                    return Ok(Result<string>.Success("Xóa bài học thành công!"));
                }
                else
                {
                    return StatusCode(500, Result<object>.Failure(new[] { "Xóa bài học thất bại." }));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        public record CreateLessonRequest(int CourseId, string Title);
        public record UpdateLessonRequest(string Title);
        public record LessonDto(
             int LessonId,
             string Title
         );

        public record LessonPagedResult(IEnumerable<LessonDto> Lessons, int TotalCount);

    }
}
