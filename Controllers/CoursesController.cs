using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Be_QuanLyKhoaHoc.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SampleProject.Common;
using System.Text.Json.Serialization;
using Be_QuanLyKhoaHoc.Services;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly S3Service _s3Service;
        private readonly CloudFrontService _cloudFrontService;
        private readonly DeleteService _deleteService;

        public CoursesController(ApplicationDbContext context, S3Service s3Service, CloudFrontService cloudFrontService, DeleteService deleteService)
        {
            _context = context;
            _s3Service = s3Service;
            _cloudFrontService = cloudFrontService;
            _deleteService = deleteService;
        }
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetCourseTitleById(int id)
        {
            try
            {
                // Truy vấn lấy ra thuộc tính Title của course có CourseId = id
                var courseTitle = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.CourseId == id)
                    .Select(c => c.Title)
                    .FirstOrDefaultAsync();

                if (courseTitle == null)
                {
                    // Nếu không tìm thấy course nào thì trả về NotFound
                    return NotFound(Result<object>.Failure(new[] { "Course not found." }));
                }

                return Ok(Result<object>.Success(courseTitle));
            }
            catch (Exception ex)
            {
                // Xử lý lỗi, trả về mã 500 kèm thông tin lỗi
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // GET: api/Courses/details/{id}
        [HttpGet("details/{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<CourseDto>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetCourseDetailsById(int id)
        {
            try
            {
                var course = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.CourseId == id)
                    .Select(c => new CourseDto(
                        c.CourseId,
                        c.Title,
                        c.Description,
                        c.Price,
                        c.Difficulty,
                        c.Keywords,
                        c.AvatarUrl
                    ))
                    .FirstOrDefaultAsync();

                if (course == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                return Ok(Result<CourseDto>.Success(course));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }


        // GET: api/Courses/public
        [HttpGet("public")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<CoursePagedResult>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetCoursesForUser(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string options = null)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                var query = _context.Courses.AsNoTracking()
                    .Where(c => string.IsNullOrEmpty(options) ||
                                EF.Functions.Like(c.Title, $"%{options}%") ||
                                EF.Functions.Like(c.Description, $"%{options}%") ||
                                EF.Functions.Like(c.Keywords, $"%{options}%"));

                var totalCount = await query.CountAsync();
                if (totalCount == 0)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                if (page > totalPages) page = totalPages;

                var courses = await query
                    .OrderBy(c => c.CourseId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CourseDto(
                        c.CourseId,
                        c.Title,
                        c.Description,
                        c.Price,
                        c.Difficulty,
                        c.Keywords,
                        c.AvatarUrl
                    ))
                    .ToListAsync();

                var result = new CoursePagedResult(courses, totalCount);
                return Ok(Result<CoursePagedResult>.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }


        // GET: api/Courses
        [HttpGet]
        [ProducesResponseType(typeof(Result<CoursePagedResult>), 200)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(object), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetCoursesForLecturer([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string options = null)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 10;

                var query = _context.Courses.AsNoTracking()
                    .Where(c => c.LecturerId == lecturerId &&
                                (string.IsNullOrEmpty(options) ||
                                 EF.Functions.Like(c.Title, $"%{options}%") ||
                                 EF.Functions.Like(c.Description, $"%{options}%") ||
                                 EF.Functions.Like(c.Keywords, $"%{options}%")));

                var totalCount = await query.CountAsync();

                if (totalCount == 0)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy người dùng." }));
                }

                // Tính tổng số trang
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // Đảm bảo page không vượt quá tổng số trang
                if (page > totalPages) page = totalPages;

                var courses = await query
                    .OrderBy(c => c.CourseId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CourseDto(
                        c.CourseId,
                        c.Title,
                        c.Description,
                        c.Price,
                        c.Difficulty,
                        c.Keywords,
                        c.AvatarUrl
                    ))
                    .ToListAsync();

                if (!courses.Any())
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                var result = new CoursePagedResult(courses, totalCount);

                return Ok(Result<CoursePagedResult>.Success(result));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }


        // POST: api/Courses
        [HttpPost]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                                       .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                                       .ToArray();
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
                await _context.Courses.AddAsync(newCourse);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Tạo mới khóa học thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // PUT: api/Courses/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<string>), 200)] // Success message
        [ProducesResponseType(typeof(Result<object>), 400)] // Validation failure
        [ProducesResponseType(typeof(Result<object>), 404)] // Not found
        [ProducesResponseType(typeof(Result<object>), 401)] // Unauthorized
        [ProducesResponseType(typeof(Result<object>), 403)] // Forbidden
        [ProducesResponseType(typeof(Result<object>), 500)] // Internal server error
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseRequest request)
        {
            // Lấy thông tin giảng viên từ claims
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                                       .SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                                       .ToArray();
                return BadRequest(Result<object>.Failure(errors));
            }

            try
            {
                var existingCourse = await _context.Courses.FindAsync(id);
                if (existingCourse == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }
                if (existingCourse.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền cập nhật khóa học này." }));
                }

                existingCourse.Title = request.Title;
                existingCourse.Description = request.Description;
                existingCourse.Price = request.Price;
                existingCourse.Difficulty = request.Difficulty;
                existingCourse.Keywords = request.Keywords;
                existingCourse.AvatarUrl = request.AvatarUrl;

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
                var course = await _context.Courses
                    .Include(c => c.Lessons)
                    .Include(c => c.Exam)
                    .FirstOrDefaultAsync(c => c.CourseId == id);

                if (course == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                if (course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xoá khóa học này." }));
                }

                // ==== 1. XÓA PROGRESS ====
                await _context.Progresses
                    .Where(p => p.CourseId == id)
                    .ExecuteDeleteAsync();

                // ==== 2. XÓA EXAM ====
                var exams = await _context.Exams
                    .Where(e => e.CourseId == id)
                    .ToListAsync();

                foreach (var exam in exams)
                {
                    var result = await _deleteService.DeleteExamAsync(exam.ExamId, lecturerId);
                    if (!result.Succeeded) return StatusCode(500, Result<object>.Failure(result.Errors));
                }

                // ==== 3. XÓA LESSONS ====
                var lessons = await _context.Lessons
                    .Where(l => l.CourseId == id)
                    .ToListAsync();

                foreach (var lesson in lessons)
                {
                    var result = await _deleteService.DeleteLessonAsync(lesson.LessonId, lecturerId);
                    if (!result.Succeeded) return StatusCode(500, Result<object>.Failure(result.Errors));
                }

                // ==== 4. XÓA FILE ẢNH TRÊN S3 (nếu có) ====
                if (!string.IsNullOrEmpty(course.AvatarUrl))
                {
                    string objectKey = ExtractS3ObjectKey(course.AvatarUrl);
                    if (!string.IsNullOrEmpty(objectKey))
                    {
                        await _s3Service.DeleteS3ObjectAsync(objectKey);
                    }
                }

                // ==== 5. XÓA KHÓA HỌC ====
                await _context.Courses
                    .Where(p => p.CourseId == id)
                    .ExecuteDeleteAsync();

                return Ok(Result<string>.Success("Xoá khóa học và tất cả dữ liệu liên quan thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        [HttpPut("{courseId}/avatar-course")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> UpdateCourseAvatar(int courseId, [FromBody] UpdateAvatarRequest request)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            if (string.IsNullOrEmpty(request.AvatarObjectKey))
            {
                return BadRequest(Result<object>.Failure(new[] { "AvatarObjectKey không được để trống." }));
            }

            try
            {
                // Kiểm tra khóa học và quyền sở hữu của giảng viên
                var course = await _context.Courses
                    .Where(c => c.CourseId == courseId && c.LecturerId == lecturerId)
                    .FirstOrDefaultAsync();

                if (course == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học hoặc không có quyền chỉnh sửa." }));
                }

                // Xử lý xóa avatar cũ nếu tồn tại và khác với avatar mới
                if (!string.IsNullOrEmpty(course.AvatarUrl))
                {
                    string oldObjectKey = ExtractS3ObjectKey(course.AvatarUrl);
                    if (!string.IsNullOrEmpty(oldObjectKey) && oldObjectKey != request.AvatarObjectKey)
                    {
                        await _s3Service.DeleteS3ObjectAsync(oldObjectKey);
                    }
                }

                // Cập nhật AvatarUrl với avatar mới
                string avatarUrl = _cloudFrontService.GetCloudFrontUrl(request.AvatarObjectKey);
                course.AvatarUrl = avatarUrl;

                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Cập nhật avatar thành công."));
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
        public record CourseDto(
            int CourseId,
            string Title,
            string? Description,
            double? Price,
            string? Difficulty,
            string? Keywords,
            string? AvatarUrl
        );
        public record UpdateAvatarRequest
        (
            string AvatarObjectKey
        );
        public record CoursePagedResult(IEnumerable<CourseDto> Courses, int TotalCount);

        private string ExtractS3ObjectKey(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string path = uri.AbsolutePath;

                if (string.IsNullOrEmpty(path) || path == "/")
                {
                    return string.Empty;
                }

                return path.TrimStart('/');
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
