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
    public class ExamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExamsController(ApplicationDbContext context)
        {
            _context = context;
        }

        //Lấy danh sách bài kiểm tra theo khóa học
        [HttpGet("course/{courseId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<IEnumerable<object>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]

        public async Task<IActionResult> GetExamsByCourse(int courseId)
        {
            try
            {
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy khóa học." }));
                }

                var exams = await _context.Exams
                    .Where(e => e.CourseId == courseId)
                    .Select(e => new { e.ExamId, e.Title })
                    .ToListAsync();

                return Ok(Result<IEnumerable<object>>.Success(exams));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        //Lấy chi tiết bài kiểm tra
        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<Exam>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]

        public async Task<IActionResult> GetExam(int id)
        {
            try
            {
                var exam = await _context.Exams
                    .Include(e => e.MultipleChoiceQuestions)
                    .Include(e => e.FillInBlankQuestions)
                    .FirstOrDefaultAsync(e => e.ExamId == id);

                if (exam == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                return Ok(Result<Exam>.Success(exam));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Tạo mới bài kiểm tra
        [HttpPost]
        [ProducesResponseType(typeof(Result<Exam>), 201)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]

        public async Task<IActionResult> CreateExam([FromBody] CreateExamRequest request)
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
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền tạo bài kiểm tra cho khóa học này." }));
                }

                var newExam = new Exam
                {
                    Title = request.Title,
                    Description = request.Description,
                    CourseId = request.CourseId
                };

                _context.Exams.Add(newExam);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetExam), new { id = newExam.ExamId }, Result<Exam>.Success(newExam));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        //Cập nhật bài kiểm tra
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]

        public async Task<IActionResult> UpdateExam(int id, [FromBody] UpdateExamRequest request)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var exam = await _context.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.ExamId == id);
                if (exam == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                if (exam.Course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền chỉnh sửa bài kiểm tra này." }));
                }

                exam.Title = request.Title;
                exam.Description = request.Description;

                _context.Entry(exam).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Cập nhật bài kiểm tra thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // Xóa bài kiểm tra
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 401)]

        public async Task<IActionResult> DeleteExam(int id)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var exam = await _context.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.ExamId == id);
                if (exam == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                if (exam.Course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xóa bài kiểm tra này." }));
                }

                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Xóa bài kiểm tra thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        public record CreateExamRequest(int CourseId, string Title, string? Description);
        public record UpdateExamRequest(string Title, string? Description);



        // Người học nộp kết quả bài kiểm tra
        [HttpPost("{examId}/submit")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        public async Task<IActionResult> SubmitExam(int examId, [FromBody] SubmitExamRequest request)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin người học không hợp lệ." }));
            }

            try
            {
                var exam = await _context.Exams.FindAsync(examId);
                if (exam == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                var examResult = new ExamResult
                {
                    StudentId = studentId,
                    ExamId = examId,
                    Score = request.Score,
                    SubmissionTime = DateTime.UtcNow
                };

                _context.ExamResults.Add(examResult);
                await _context.SaveChangesAsync();

                return Ok(Result<string>.Success("Nộp bài kiểm tra thành công!"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        // Người học xem kết quả bài kiểm tra của mình
        [HttpGet("{examId}/result")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetExamResult(int examId)
        {
            var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin người học không hợp lệ." }));
            }

            try
            {
                var examResult = await _context.ExamResults
                    .Where(er => er.ExamId == examId && er.StudentId == studentId)
                    .Select(er => new
                    {
                        er.ResultId,
                        er.Score,
                        er.SubmissionTime
                    })
                    .FirstOrDefaultAsync();

                if (examResult == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Bạn chưa có kết quả cho bài kiểm tra này." }));
                }

                return Ok(Result<object>.Success(examResult));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        // Giảng viên xem danh sách kết quả của học viên
        [HttpGet("{examId}/results")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [ProducesResponseType(typeof(Result<string>), 200)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        public async Task<IActionResult> GetExamResults(int examId)
        {
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                var exam = await _context.Exams.Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.ExamId == examId);

                if (exam == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy bài kiểm tra." }));
                }

                if (exam.Course.LecturerId != lecturerId)
                {
                    return StatusCode(403, Result<object>.Failure(new[] { "Bạn không có quyền xem kết quả bài kiểm tra này." }));
                }

                var results = await _context.ExamResults
                    .Where(er => er.ExamId == examId)
                    .Include(er => er.Student)
                    .Select(er => new
                    {
                        er.ResultId,
                        StudentName = er.Student!.FullName,
                        er.Score,
                        er.SubmissionTime
                    })
                    .ToListAsync();

                return Ok(Result<IEnumerable<object>>.Success(results));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Lỗi hệ thống: {ex.Message}" }));
            }
        }

        // Định nghĩa request model
        public record SubmitExamRequest(float Score);


    }
}
