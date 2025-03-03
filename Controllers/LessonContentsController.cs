using Be_QuanLyKhoaHoc.Identity;
using Be_QuanLyKhoaHoc.Identity.Entities;
using Be_QuanLyKhoaHoc.Services;
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
        private readonly S3Service _s3Service;
        public LessonContentsController(ApplicationDbContext context, S3Service s3Service)
        {
            _context = context;
            _s3Service = s3Service;
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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

                if (string.IsNullOrEmpty(request.Content) && string.IsNullOrEmpty(request.MediaUrl))
                {
                    return BadRequest(Result<object>.Failure(new[] { "Phải có ít nhất nội dung văn bản hoặc file phương tiện." }));
                }

                string mediaUrl = null;
                string cloudFrontDomain = "https://drui9ols58b43.cloudfront.net";

                // Nếu là ảnh -> Lấy URL từ CloudFront dựa trên ObjectKey từ S3
                if (request.MediaType == "image" && !string.IsNullOrEmpty(request.MediaUrl))
                {
                    mediaUrl = $"{cloudFrontDomain}/{request.MediaUrl}"; // request.MediaUrl lúc này là objectKey từ S3
                }
                // Nếu là video -> Dùng URL trực tiếp
                else if (request.MediaType == "video" && !string.IsNullOrEmpty(request.MediaUrl))
                {
                    mediaUrl = request.MediaUrl; // Giữ nguyên cách lưu video
                }
                else if (!string.IsNullOrEmpty(request.MediaUrl))
                {
                    return BadRequest(Result<object>.Failure(new[] { "MediaType không hợp lệ. Chỉ hỗ trợ 'image' hoặc 'video'." }));
                }

                var lessonContent = new LessonContent
                {
                    LessonId = request.LessonId,
                    MediaType = request.MediaType,
                    MediaUrl = mediaUrl,
                    Content = request.Content
                };

                _context.LessonContents.Add(lessonContent);
                await _context.SaveChangesAsync();

                return Ok(Result<object>.Success("Thêm nội dung cho bài học thành công."));
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

                // Nếu URL media mới khác URL cũ => xóa file cũ (chỉ ảnh, không phải video)
                if (!string.IsNullOrEmpty(lessonContent.MediaUrl) && lessonContent.MediaUrl != request.MediaUrl)
                {
                    string objectKey = ExtractS3ObjectKey(lessonContent.MediaUrl);

                    if (!string.IsNullOrEmpty(objectKey))
                    {
                        await _s3Service.DeleteS3ObjectAsync(objectKey);
                    }
                    else
                    {
                        DeleteFileIfExists(lessonContent.MediaUrl); // Xóa file cục bộ
                    }
                }

                // Cập nhật nội dung
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

                if (!string.IsNullOrEmpty(lessonContent.MediaUrl))
                {
                    string objectKey = ExtractS3ObjectKey(lessonContent.MediaUrl);

                    if (!string.IsNullOrEmpty(objectKey) && !objectKey.Contains("uploads/"))
                    {
                        await _s3Service.DeleteS3ObjectAsync(objectKey);
                    }
                    else
                    {
                        DeleteFileIfExists(lessonContent.MediaUrl);
                    }
                }


                // Xóa bản ghi trong database
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
