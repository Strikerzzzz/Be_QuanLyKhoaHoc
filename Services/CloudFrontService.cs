using System;
using System.Security.Cryptography;
using System.Text;

namespace Be_QuanLyKhoaHoc.Services
{
    public class CloudFrontService
    {
        private readonly string _cloudFrontDomain;
        private readonly string _publicKeyId;
        private readonly string _privateKeyFilePath;

        public CloudFrontService(IConfiguration configuration)
        {
            _cloudFrontDomain = configuration["CloudFront:Domain"]
                ?? throw new ArgumentNullException("CloudFront:Domain không được cấu hình.");
            _publicKeyId = configuration["CloudFront:PublicKeyId"]
                ?? throw new ArgumentNullException("CloudFront:PublicKeyId không được cấu hình.");
            _privateKeyFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Keys", "private_key.pem");
            if (!File.Exists(_privateKeyFilePath))
            {
                throw new FileNotFoundException("Không tìm thấy file private key tại: " + _privateKeyFilePath);
            }
        }

        public string GetCloudFrontUrl(string objectKey)
        {
            if (string.IsNullOrEmpty(objectKey))
            {
                throw new ArgumentException("ObjectKey không được để trống.");
            }

            return $"{_cloudFrontDomain}/{objectKey}";
        }

        /// <summary>
        /// Tạo CloudFront Signed URL dựa trên canned policy với thời gian hết hạn cho trước,
        /// sử dụng PublicKeyId (trong Key Group) và ký bằng PrivateKey tương ứng.
        /// </summary>
        /// <param name="objectKey">Định danh của file trên CloudFront (thường là S3 key)</param>
        /// <param name="expires">Thời gian hết hạn của URL</param>
        /// <returns>URL đã được ký</returns>
        public string GenerateSignedUrl(string objectKey, DateTime expires)
        {
            if (string.IsNullOrEmpty(objectKey))
            {
                throw new ArgumentException("ObjectKey không được để trống.");
            }

            // Tạo resource URL: kết hợp domain CloudFront và objectKey
            string resourceUrl = GetCloudFrontUrl(objectKey);
            // Lấy thời gian hết hạn dưới dạng epoch seconds
            long expiresEpoch = ((DateTimeOffset)expires).ToUnixTimeSeconds();

            // Tạo canned policy dạng JSON
            string policy = $"{{\"Statement\":[{{\"Resource\":\"{resourceUrl}\",\"Condition\":{{\"DateLessThan\":{{\"AWS:EpochTime\":{expiresEpoch}}}}}}}]}}";

            // Ký policy bằng private key
            string signature = SignPolicy(policy);

            // Xây dựng URL cuối cùng với các tham số: Expires, Signature và Key-Pair-Id (sử dụng PublicKeyId)
            string signedUrl = $"{resourceUrl}?Expires={expiresEpoch}&Signature={Uri.EscapeDataString(signature)}&Key-Pair-Id={_publicKeyId}";
            return signedUrl;
        }

        /// <summary>
        /// Ký policy JSON bằng RSA SHA1 với PrivateKey.
        /// Sau đó, chuyển đổi chữ ký sang Base64 và làm URL-safe.
        /// </summary>
        private string SignPolicy(string policy)
        {
            try
            {
                using (var rsa = RSA.Create())
                {
                    // Đọc nội dung file private key
                    string privateKeyContent = File.ReadAllText(_privateKeyFilePath);
                    rsa.ImportFromPem(privateKeyContent.ToCharArray());

                    // Ký policy
                    byte[] policyBytes = Encoding.UTF8.GetBytes(policy);
                    byte[] signatureBytes = rsa.SignData(policyBytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                    string signature = Convert.ToBase64String(signatureBytes);

                    // Chuyển đổi thành URL-safe
                    signature = signature.Replace("+", "-").Replace("/", "~").Replace("=", "_");
                    return signature;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi ký policy: {ex.Message}");
                throw;
            }
        }
    }
}