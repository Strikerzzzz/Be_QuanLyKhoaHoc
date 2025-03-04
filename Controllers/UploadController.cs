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
        private readonly VideoConverterService _videoConverterService;

        public UploadController(S3Service s3Service, VideoConverterService videoConverterService)
        {
            _s3Service = s3Service;
            _videoConverterService = videoConverterService;
        }

        public record UploadResponse(
            string PresignedUrl,
            string ObjectKey
            );

        // Endpoint: GET /api/avatar/presigned-url?courseId=123&fileName=avatar.png&contentType=image/png
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [HttpGet("presigned-url")]
        [ProducesResponseType(typeof(Result<UploadResponse>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 401)]
        [ProducesResponseType(typeof(Result<object>), 403)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> GetPresignedUrl([FromQuery] string fileName, [FromQuery] string contentType, [FromQuery] string type)
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

            try
            {
                string folder = type.ToLower() == "avatar" ? "images/avatars" : "images/content";
                string objectKey = $"{folder}/{Guid.NewGuid()}_{fileName}";

                string presignedUrl = await _s3Service.GeneratePresignedUrlAsync(objectKey, contentType);

                // Trả về URL upload và objectKey (để lưu vào DB sau này)
                return Ok(Result<UploadResponse>.Success(new UploadResponse(presignedUrl, objectKey)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

        [HttpPost("upload")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Lecturer")]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        [ProducesResponseType(typeof(Result<object>), 200)]
        [ProducesResponseType(typeof(Result<object>), 400)]
        [ProducesResponseType(typeof(Result<object>), 500)]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(Result<object>.Failure(new[] { "Video không hợp lệ." }));
            }

            // Kiểm tra kích thước file vượt quá giới hạn 500 MB
            if (file.Length > 524288000)
            {
                return BadRequest(Result<object>.Failure(new[] { "Kích thước video vượt quá giới hạn cho phép (500MB)." }));
            }

            // Danh sách định dạng video hợp lệ
            var allowedExtensions = new HashSet<string> { ".mp4", ".mov", ".avi", ".mkv" };
            var allowedContentTypes = new HashSet<string> { "video/mp4", "video/quicktime", "video/x-msvideo", "video/x-matroska" };

            // Kiểm tra phần mở rộng của file
            string fileExtension = Path.GetExtension(file.FileName)?.ToLower();
            if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(Result<object>.Failure(new[] { "Định dạng video không hợp lệ. Chỉ chấp nhận video (.mp4, .mov, .avi, .mkv)." }));
            }

            // Kiểm tra contentType của file
            if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest(Result<object>.Failure(new[] { "Loại tệp không hợp lệ. Chỉ chấp nhận video." }));
            }

            try
            {
                // Đường dẫn lưu file chuyên biệt cho video
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/videos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                var videoFolderName = Path.GetFileNameWithoutExtension(uniqueFileName);
                var hlsOutputFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/hls", videoFolderName);
                bool conversionSuccess = await _videoConverterService.ConvertVideoToHLS(filePath, hlsOutputFolder);

                if (!conversionSuccess)
                {
                    return StatusCode(500, Result<object>.Failure(new[] { "Chuyển đổi video sang HLS thất bại." }));
                }

                // 3. Upload tất cả các file HLS từ thư mục lên S3
                var hlsFiles = Directory.GetFiles(hlsOutputFolder);
                foreach (var localFile in hlsFiles)
                {
                    string fileName = Path.GetFileName(localFile);
                    string s3Key = $"videos/hls/{videoFolderName}/{fileName}";

                    string contentType = fileName.EndsWith(".m3u8")
                        ? "application/x-mpegURL"
                        : fileName.EndsWith(".ts") ? "video/MP2T" : "application/octet-stream";

                    await _s3Service.UploadFileAsync(s3Key, localFile, contentType);
                }
                try
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    if (Directory.Exists(hlsOutputFolder))
                    {
                        Directory.Delete(hlsOutputFolder, true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi xóa file tạm: {ex.Message}");
                }
                string mediaUrlKey = $"videos/hls/{videoFolderName}/output.m3u8";

                return Ok(Result<object>.Success(new { url = mediaUrlKey }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Result<object>.Failure(new[] { $"Có lỗi xảy ra: {ex.Message}" }));
            }
        }

    }
}
