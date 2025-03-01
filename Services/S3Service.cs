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

        public async Task<string> GeneratePresignedUrlAsync(string objectKey, string contentType)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,  // Chỉ cho phép PUT để upload
                Expires = DateTime.UtcNow.AddMinutes(10),
                ContentType = contentType
            };

            return _s3Client.GetPreSignedURL(request);
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
    }
}
