using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Be_QuanLyKhoaHoc.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<S3Service> _logger;

        public S3Service(IConfiguration configuration, ILogger<S3Service> logger)
        {
            var awsOptions = configuration.GetSection("AWS");
            _bucketName = awsOptions["BucketName"] ?? throw new ArgumentNullException("AWS:BucketName");
            _s3Client = new AmazonS3Client(
                awsOptions["AccessKey"],
                awsOptions["SecretKey"],
                Amazon.RegionEndpoint.GetBySystemName(awsOptions["Region"])
            );
            _logger = logger;
        }

        public Task<string> GeneratePresignedUrlAsync(string objectKey, string contentType)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,  // Chỉ cho phép PUT để upload
                Expires = DateTime.UtcNow.AddMinutes(10),
                ContentType = contentType
            };

            string presignedUrl = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(presignedUrl);
        }

        public async Task DeleteS3ObjectAsync(string objectKey)
        {
            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = objectKey
                };

                var response = await _s3Client.DeleteObjectAsync(deleteRequest);
                _logger.LogInformation($"Đã xóa file trên S3: {objectKey}, HTTP Status: {response.HttpStatusCode}");
            }
            catch (AmazonS3Exception s3Ex)
            {
                _logger.LogError($"Lỗi AWS S3 khi xóa file {objectKey}: {s3Ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi không xác định khi xóa file {objectKey} trên S3: {ex.Message}");
            }
        }

        public async Task DeleteS3DirectoryAsync(string directoryKey)
        {
            try
            {
                ListObjectsV2Request listRequest = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = directoryKey
                };

                ListObjectsV2Response listResponse = await _s3Client.ListObjectsV2Async(listRequest);

                if (listResponse.S3Objects.Any())
                {
                    DeleteObjectsRequest deleteRequest = new DeleteObjectsRequest
                    {
                        BucketName = _bucketName,
                        Objects = listResponse.S3Objects.Select(o => new KeyVersion { Key = o.Key }).ToList()
                    };

                    await _s3Client.DeleteObjectsAsync(deleteRequest);
                    _logger.LogInformation($"Đã xóa thư mục trên S3: {directoryKey}");
                }
                else
                {
                    _logger.LogInformation($"Thư mục trên S3: {directoryKey} không có object nào để xóa.");
                }
            }
            catch (AmazonS3Exception s3Ex)
            {
                _logger.LogError($"Lỗi AWS S3 khi xóa thư mục {directoryKey}: {s3Ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi không xác định khi xóa thư mục {directoryKey} trên S3: {ex.Message}");
                throw;
            }
        }

        public async Task UploadFileAsync(string key, string filePath, string contentType)
        {
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    FilePath = filePath,
                    ContentType = contentType
                };

                await _s3Client.PutObjectAsync(putRequest);
                _logger.LogInformation($"Đã upload file lên S3: {key}");
            }
            catch (AmazonS3Exception s3Ex)
            {
                _logger.LogError($"Lỗi AWS S3 khi upload file {key}: {s3Ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi không xác định khi upload file {key} lên S3: {ex.Message}");
                throw;
            }
        }

        public async Task UploadHLSDirectoryAsync(string directoryPath, string s3BaseKey)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath);
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string key = $"{s3BaseKey}/{fileName}";

                    // Xác định ContentType dựa trên đuôi file
                    string contentType = fileName.EndsWith(".m3u8")
                        ? "application/x-mpegURL"
                        : fileName.EndsWith(".ts") ? "video/MP2T" : "application/octet-stream";

                    await UploadFileAsync(key, file, contentType);
                }
                _logger.LogInformation($"Đã upload thư mục HLS lên S3: {s3BaseKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi upload thư mục HLS {directoryPath} lên S3: {ex.Message}");
                throw;
            }
        }

    }
}
