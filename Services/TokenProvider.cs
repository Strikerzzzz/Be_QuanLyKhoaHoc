using Be_QuanLyKhoaHoc.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Be_QuanLyKhoaHoc.Services
{
    public sealed class TokenProvider
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;

        public TokenProvider(IConfiguration configuration, UserManager<User> userManager)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<string> Create(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            string secretKey = _configuration["Jwt:Secret"];

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT secret key is not configured.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Tự động lấy vai trò của user từ UserManager
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles.Distinct())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:ExpirationInMinutes")),
                SigningCredentials = credentials,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
            };

            var handler = new JsonWebTokenHandler();
            string token = handler.CreateToken(tokenDescriptor);

            return token;
        }
        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber).Substring(0, 43);
        }
    }
}
