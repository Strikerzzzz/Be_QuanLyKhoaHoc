using Be_QuanLyKhoaHoc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SampleProject.Common;
using System.Security.Claims;

namespace Be_QuanLyKhoaHoc.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly S3Service _s3Service;

        public UploadController(S3Service s3Service)
        {
            _s3Service = s3Service;
        }

        public record AvatarUploadResponse(
            string PresignedUrl,
            string ObjectKey
            );

        // Endpoint: GET /api/avatar/presigned-url?courseId=123&fileName=avatar.png&contentType=image/png
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [HttpGet("avatar-presigned-url")]
        [ProducesResponseType(typeof(Result<AvatarUploadResponse>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetAvatarPresignedUrl([FromQuery] int courseId, [FromQuery] string fileName, [FromQuery] string contentType)
        {
            // Kiểm tra thông tin đầu vào
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(contentType))
            {
                return BadRequest(Result<object>.Failure(new[] { "Thiếu thông tin fileName hoặc contentType." }));
            }

            // Danh sách các định dạng ảnh hợp lệ
            var allowedExtensions = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var allowedContentTypes = new HashSet<string> { "image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp" };

            // Kiểm tra phần mở rộng của fileName
            string fileExtension = Path.GetExtension(fileName)?.ToLower();
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(Result<object>.Failure(new[] { "Định dạng file không hợp lệ. Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .gif, .bmp, .webp" }));
            }

            // Kiểm tra contentType
            if (!allowedContentTypes.Contains(contentType.ToLower()))
            {
                return BadRequest(Result<object>.Failure(new[] { "Loại tệp không hợp lệ. Chỉ chấp nhận các loại ảnh hợp lệ." }));
            }

            // Xác thực giảng viên: lấy lecturerId từ JWT
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                // Sử dụng folder cố định cho avatar, ví dụ: images/avatars
                string objectKey = $"images/avatars/{Guid.NewGuid()}_{fileName}";

                string presignedUrl = await _s3Service.GeneratePresignedUrlAsync(objectKey, contentType);

                // Trả về URL upload và objectKey (để lưu vào DB sau này)
                return Ok(Result<AvatarUploadResponse>.Success(new AvatarUploadResponse(presignedUrl, objectKey)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }


    }
}
