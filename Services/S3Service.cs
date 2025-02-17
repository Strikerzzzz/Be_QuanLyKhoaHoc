using Amazon.S3;
using Amazon.S3.Model;

namespace Be_QuanLyKhoaHoc.Services
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        public S3Service(IConfiguration configuration)
        {
            var awsOptions = configuration.GetSection("AWS");
            _bucketName = awsOptions["BucketName"];
            _s3Client = new AmazonS3Client(
                awsOptions["AccessKey"],
                awsOptions["SecretKey"],
                Amazon.RegionEndpoint.GetBySystemName(awsOptions["Region"])
            );
        }

        public async Task<string> GeneratePresignedUrlAsync(string objectKey, string contentType)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,  // Chỉ cho phép PUT để upload
                Expires = DateTime.UtcNow.AddMinutes(10), // URL có hiệu lực trong 10 phút
                ContentType = contentType
            };

            return _s3Client.GetPreSignedURL(request);
        }
    }
}
