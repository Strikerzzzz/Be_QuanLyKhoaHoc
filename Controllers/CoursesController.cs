using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Be_QuanLyKhoaHoc.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SampleProject.Common;
using System.Text.Json.Serialization;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Courses
        [HttpGet]
        [ProducesResponseType(typeof(Result<IEnumerable<Course>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetCourses()
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var courses = await _context.Courses
                    .Where(c => c.LecturerId == lecturerId)
                    .Include(c => c.Lecturer)
                    .ToListAsync();
                return Ok(Result<IEnumerable<Course>>.Success(courses));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // GET: api/Courses/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<Course>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetCourse(int id)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                // Truy vấn khóa học không ràng buộc lecturerId để phân biệt giữa không tồn tại và không có quyền truy cập.
                var course = await _context.Courses
                    .Include(c => c.Lecturer)
                    .FirstOrDefaultAsync(c => c.CourseId == id);

                if (course == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                if (course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền truy cập khóa học này." }));
                }

                return Ok(Result<Course>.Success(course));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // POST: api/Courses
        [HttpPost]
        [ProducesResponseType(typeof(Result<Course>), 201)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
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
                var newCourse = new Course
                {
                    Title = request.Title,
                    Description = request.Description,
                    Price = request.Price,
                    Difficulty = request.Difficulty,
                    Keywords = request.Keywords,
                    AvatarUrl = request.AvatarUrl,
                    LecturerId = lecturerId
                };

                _context.Courses.Add(newCourse);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetCourse), new { id = newCourse.CourseId }, Result<Course>.Success(newCourse));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // PUT: api/Courses/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseRequest request)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                // Truy vấn khóa học không ràng buộc lecturerId để phân biệt lỗi không tồn tại và lỗi không có quyền.
                var existingCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == id);
                if (existingCourse == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                if (existingCourse.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền cập nhật khóa học này." }));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToArray();
                    return BadRequest(Result<object>.Failure(errors));
                }

                existingCourse.Title = request.Title;
                existingCourse.Description = request.Description;
                existingCourse.Price = request.Price;
                existingCourse.Difficulty = request.Difficulty;
                existingCourse.Keywords = request.Keywords;
                existingCourse.AvatarUrl = request.AvatarUrl;

                _context.Entry(existingCourse).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Cập nhật khóa học thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // DELETE: api/Courses/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                // Truy vấn khóa học không ràng buộc lecturerId để phân biệt lỗi không tồn tại và lỗi không có quyền.
                var course = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == id);
                if (course == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                if (course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xoá khóa học này." }));
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                return Ok(Result<string>.Success("Xoá khóa học thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }


        public record CreateCourseRequest(
            string Title,
            string? Description,
            double? Price,
            string? Difficulty,
            string? Keywords,
            string? AvatarUrl
        );

        public record UpdateCourseRequest(
            string Title,
            string? Description,
            double? Price,
            string? Difficulty,
            string? Keywords,
            string? AvatarUrl
        );
    }
}
