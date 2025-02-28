using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleProject.Common;
using System.Data;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
    public class LessonContentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public LessonContentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/LessonContents/{lessonId}
        [HttpGet("{lessonId}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "User")]
        [ProducesResponseType(typeof(Result<IEnumerable<ContentDto>>), 200)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetLessonContentsByLessonId(int lessonId)
        {
            try
            {
                var contentDtos = await _context.LessonContents
                    .AsNoTracking()
                    .Where(lc => lc.LessonId == lessonId)
                    .Select(lc => new ContentDto(
                        lc.LessonContentId,
                        lc.LessonId,
                        lc.MediaType,
                        lc.MediaUrl,
                        lc.Content))
                    .ToListAsync();

                return Ok(Result<IEnumerable<ContentDto>>.Success(contentDtos));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // POST: api/LessonContents
        [HttpPost]
        [ProducesResponseType(typeof(Result<object>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> CreateLessonContent([FromBody] CreateLessonContentRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(Result<object>.Failure(new[] { "Dữ liệu không hợp lệ." }));
                }

                var lessonContent = new LessonContent
                {
                    LessonId = request.LessonId,
                    MediaType = request.MediaType,
                    MediaUrl = request.MediaUrl,
                    Content = request.Content
                };

                _context.LessonContents.Add(lessonContent);
                await _context.SaveChangesAsync();

                // Trả về HTTP 200 với thông báo thành công
                return Ok(Result<object>.Success("Thêm nội dung cho khóa học thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        // PUT: api/LessonContents/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> UpdateLessonContent(int id, [FromBody] UpdateLessonContentRequest request)
        {
            try
            {
                if (id != request.LessonContentId)
                {
                    return BadRequest(Result<object>.Failure(new[] { "ID không khớp." }));
                }

                var lessonContent = await _context.LessonContents
                    .FirstOrDefaultAsync(lc => lc.LessonContentId == id);

                if (lessonContent == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy nội dung bài học." }));
                }

                // Nếu URL media mới khác URL cũ => xóa file cũ
                if (!string.IsNullOrEmpty(lessonContent.MediaUrl) && lessonContent.MediaUrl != request.MediaUrl)
                {
                    DeleteFileIfExists(lessonContent.MediaUrl);
                }
                lessonContent.MediaType = request.MediaType;
                lessonContent.MediaUrl = request.MediaUrl;
                lessonContent.Content = request.Content;

                await _context.SaveChangesAsync();

                return Ok(Result<object>.Success("Cập nhật thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }


        // DELETE: api/LessonContents/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(Result<object>), 200)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 404)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> DeleteLessonContent(int id)
        {
            try
            {
                var lessonContent = await _context.LessonContents
                    .AsNoTracking()
                    .FirstOrDefaultAsync(lc => lc.LessonContentId == id);

                if (lessonContent == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy nội dung bài học." }));
                }
                DeleteFileIfExists(lessonContent.MediaUrl);

                _context.LessonContents.Remove(lessonContent);
                await _context.SaveChangesAsync();

                return Ok(Result<object>.Success("Xóa thành công."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }
        private void DeleteFileIfExists(string? filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }

        public record ContentDto(
           int LessonContentId,
           int LessonId,
           string? MediaType,
           string? MediaUrl,
           string? Content
        );
        public record CreateLessonContentRequest(
            int LessonId,
            string? MediaType,
            string? MediaUrl,
            string? Content
        );
        public record UpdateLessonContentRequest(
            int LessonContentId,
            string? MediaType,
            string? MediaUrl,
            string? Content
        );
    }
}
