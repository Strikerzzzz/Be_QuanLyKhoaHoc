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

            // Xác thực giảng viên: lấy lecturerId từ JWT
            var lecturerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(lecturerId))
            {
                return Unauthorized(Result<object>.Failure(new[] { "Thông tin giảng viên không hợp lệ." }));
            }

            try
            {
                // Sử dụng folder cố định cho avatar, ví dụ: images/avatars
                // Sử dụng Guid để đảm bảo tính duy nhất và tránh ghi đè file cũ
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
