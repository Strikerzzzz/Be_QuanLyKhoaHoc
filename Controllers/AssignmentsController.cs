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
    public class AssignmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách bài tập theo bài học
        [HttpGet("lesson/{lessonId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<IEnumerable<object>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> GetAssignmentsByLesson(int lessonId)
        {
            try
            {
                var lesson = await _context.Lessons.FindAsync(lessonId);
                if (lesson == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài học." }));
                }

                var assignments = await _context.Assignments
                    .Where(a => a.LessonId == lessonId)
                    .Select(a => new { a.AssignmentId, a.Title })
                    .ToListAsync();

                return Ok(Result<IEnumerable<object>>.Success(assignments));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Lấy chi tiết bài tập
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<Assignment>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        public async Task<IActionResult> GetAssignment(int id)
        {
            try
            {
                var assignment = await _context.Assignments
                    .Include(a => a.MultipleChoiceQuestions)
                    .Include(a => a.FillInBlankQuestions)
                    .FirstOrDefaultAsync(a => a.AssignmentId == id);

                if (assignment == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                return Ok(Result<Assignment>.Success(assignment));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Tạo mới bài tập
        [HttpPost]
        [ProducesResponseType(typeof(Result<Assignment>), 201)]
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
                var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.LessonId == request.LessonId);
                if (lesson == null || lesson.Course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền tạo bài tập cho bài học này." }));
                }

                var newAssignment = new Assignment
                {
                    Title = request.Title,
                    Description = request.Description,
                    AssignmentType = request.AssignmentType,
                    LessonId = request.LessonId
                };

                _context.Assignments.Add(newAssignment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAssignment), new { id = newAssignment.AssignmentId }, Result<Assignment>.Success(newAssignment));
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
                var assignment = await _context.Assignments.Include(a => a.Lesson).ThenInclude(l => l.Course)
                    .FirstOrDefaultAsync(a => a.AssignmentId == id);

                if (assignment == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                if (assignment.Lesson.Course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền chỉnh sửa bài tập này." }));
                }

                assignment.Title = request.Title;
                assignment.Description = request.Description;
                assignment.AssignmentType = request.AssignmentType;

                _context.Entry(assignment).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Cập nhật bài tập thành công!"));
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
                var assignment = await _context.Assignments.Include(a => a.Lesson).ThenInclude(l => l.Course)
                    .FirstOrDefaultAsync(a => a.AssignmentId == id);

                if (assignment == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài tập." }));
                }

                if (assignment.Lesson.Course.LecturerId != lecturerId)
                {  
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xóa bài tập này." }));
                }

                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Xóa bài tập thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        public record CreateAssignmentRequest(int LessonId, string Title, string? Description, string? AssignmentType);
        public record UpdateAssignmentRequest(string Title, string? Description, string? AssignmentType);
    }
}
