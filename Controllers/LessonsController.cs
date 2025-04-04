using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Be_QuanLyKhoaHoc.Identity.Entities;
using SampleProject.Common;
using Be_QuanLyKhoaHoc.Identity;
using System.Linq;
using Be_QuanLyKhoaHoc.Services;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
    public class LessonsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DeleteService _deleteService;

        public LessonsController(ApplicationDbContext context, DeleteService deleteService)
        {
            _context = context;
            _deleteService = deleteService;
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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

            var result = await _deleteService.DeleteLessonAsync(id, lecturerId);

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



        public record CreateLessonRequest(int CourseId, string Title);
        public record UpdateLessonRequest(string Title);
        public record LessonDto(
             int LessonId,
             string Title
         );

        public record LessonPagedResult(IEnumerable<LessonDto> Lessons, int TotalCount);

    }
}
