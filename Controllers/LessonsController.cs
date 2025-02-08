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

        // Lấy danh sách bài học theo khóa học
        [HttpGet("course/{courseId}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<IEnumerable<Lesson>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        public async Task<IActionResult> GetLessonsByCourse(int courseId)
        {
           
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

               //lấy mỗi courseid và title
                var lessons = await _context.Lessons
                    .Where(l => l.CourseId == courseId)
                    .ToListAsync();

                return Ok(Result<IEnumerable<Lesson>>.Success(lessons));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Lấy chi tiết một bài học
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<Lesson>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        public async Task<IActionResult> GetLesson(int id)
        {
            
            try
            {
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.LessonId == id);

                if (lesson == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài học." }));
                }

              
                return Ok(Result<Lesson>.Success(lesson));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Tạo mới bài học
        [HttpPost]
        [ProducesResponseType(typeof(Result<Lesson>), 201)]
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

                _context.Lessons.Add(newLesson);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetLesson), new { id = newLesson.LessonId }, Result<Lesson>.Success(newLesson));
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
                var lesson = await _context.Lessons
                    .Include(l => l.Course)
                    .FirstOrDefaultAsync(l => l.LessonId == id);

                if (lesson == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài học." }));
                }

                if (lesson.Course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền chỉnh sửa bài học này." }));
                }

                lesson.Title = request.Title;

                _context.Entry(lesson).State = EntityState.Modified;
                await _context.SaveChangesAsync();

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
                var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.LessonId == id);
                if (lesson == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài học." }));
                }

                if (lesson.Course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xóa bài học này." }));
                }

                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Xóa bài học thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        public record CreateLessonRequest(int CourseId, string Title);
        public record UpdateLessonRequest(string Title);
    }
}
