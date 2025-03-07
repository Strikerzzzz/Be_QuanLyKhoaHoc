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
    public class LessonContentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly S3Service _s3Service;
        private readonly CloudFrontService _cloudFrontService;
        public LessonContentsController(ApplicationDbContext context, S3Service s3Service, CloudFrontService cloudFrontService)
        {
            _context = context;
            _s3Service = s3Service;
            _cloudFrontService = cloudFrontService;
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
                        lc.MediaType == "video" && !string.IsNullOrEmpty(lc.MediaUrl) && !lc.MediaUrl.StartsWith("http")
                            ? _cloudFrontService.GenerateSignedUrl(lc.MediaUrl, DateTime.UtcNow.AddMinutes(60))
                            : lc.MediaUrl,
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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

                string? mediaUrl = null;

                // Nếu là ảnh -> Lấy URL từ CloudFrontService
                if (request.MediaType == "image" && !string.IsNullOrEmpty(request.MediaUrl))
                {
                    mediaUrl = _cloudFrontService.GetCloudFrontUrl(request.MediaUrl);
                }
                // Nếu là video -> Dùng URL trực tiếp
                else if (request.MediaType == "video" && !string.IsNullOrEmpty(request.MediaUrl))
                {
                    mediaUrl = request.MediaUrl;
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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
                // Kiểm tra ID có khớp không
                if (id != request.LessonContentId)
                {
                    return BadRequest(Result<object>.Failure(new[] { "ID không khớp." }));
                }

                // Lấy nội dung bài học hiện tại từ cơ sở dữ liệu
                var lessonContent = await _context.LessonContents
                    .FirstOrDefaultAsync(lc => lc.LessonContentId == id);

                if (lessonContent == null)
                {
                    return NotFound(Result<object>.Failure(new[] { "Không tìm thấy nội dung bài học." }));
                }

                // Xử lý xóa file media cũ nếu MediaUrl thay đổi
                if (!string.IsNullOrEmpty(lessonContent.MediaUrl) && lessonContent.MediaUrl != request.MediaUrl)
                {
                    string oldObjectKey = GetObjectKey(lessonContent.MediaType, lessonContent.MediaUrl);
                    if (!string.IsNullOrEmpty(oldObjectKey))
                    {
                        if (lessonContent.MediaType == "video" && oldObjectKey.EndsWith(".m3u8"))
                        {
                            // Xóa toàn bộ thư mục chứa video HLS
                            string directoryKey = oldObjectKey.Substring(0, oldObjectKey.LastIndexOf('/') + 1);
                            await _s3Service.DeleteS3DirectoryAsync(directoryKey);
                        }
                        else
                        {
                            // Xóa object đơn lẻ (ảnh hoặc media khác)
                            await _s3Service.DeleteS3ObjectAsync(oldObjectKey);
                        }
                    }
                }

                // Cập nhật các trường mới
                lessonContent.MediaType = request.MediaType;
                lessonContent.Content = request.Content;

                // Xử lý MediaUrl mới
                if (request.MediaType == "image")
                {
                    // Giả sử MediaUrl mới là URL đầy đủ từ CloudFront
                    lessonContent.MediaUrl = request.MediaUrl;
                }
                else if (request.MediaType == "video")
                {
                    // Giả sử MediaUrl mới là đường dẫn tương đối (object key)
                    lessonContent.MediaUrl = request.MediaUrl;
                }
                else
                {
                    lessonContent.MediaUrl = null; // Trường hợp không có media
                }

                // Lưu thay đổi vào cơ sở dữ liệu
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
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
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
                    string objectKey = GetObjectKey(lessonContent.MediaType, lessonContent.MediaUrl);

                    if (!string.IsNullOrEmpty(objectKey))
                    {
                        if (lessonContent.MediaType == "video" && objectKey.EndsWith(".m3u8"))
                        {
                            // Xóa toàn bộ thư mục chứa video HLS
                            string directoryKey = objectKey.Substring(0, objectKey.LastIndexOf('/') + 1);
                            await _s3Service.DeleteS3DirectoryAsync(directoryKey);
                        }
                        else
                        {
                            // Xóa object đơn lẻ (ảnh hoặc media khác)
                            await _s3Service.DeleteS3ObjectAsync(objectKey);
                        }
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



        private string GetObjectKey(string mediaType, string mediaUrl)
        {
            if (mediaType == "image")
            {
                return ExtractS3ObjectKey(mediaUrl); // Trích xuất object key từ URL cho ảnh
            }
            else if (mediaType == "video")
            {
                return mediaUrl; // Dùng trực tiếp MediaUrl làm object key cho video
            }
            return string.Empty;
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
