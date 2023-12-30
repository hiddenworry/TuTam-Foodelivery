using DataAccess.Entities;
using DataAccess.Models.Requests.ModelBinders;
using DataAccess.Models.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BusinessLogic.Utils.SecurityServices.Implements
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public AuthenticationResponse GenerateAuthenResponse(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _config["JwtConfig:SecretKey"];
            var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            var issuer = _config["JwtConfig:Issuer"];
            int expiredTimeMinutes = _config.GetValue<int>("JwtConfig:ExpiredTimeMinutes");
            string subject = user.Id.ToString();
            var ExpireTime = SettedUpDateTime
                .GetCurrentVietNamTime()
                .AddMinutes(expiredTimeMinutes);
            var claims = new List<Claim>
            {
                new Claim("Id", user.Id.ToString()),
                new Claim("TokenId", Guid.NewGuid().ToString()),
                new Claim("DeviceId", Guid.NewGuid().ToString()),
                new Claim("ExpireTime", ExpireTime.ToString()),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? ""),
            };
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: issuer,
                claims: claims,
                expires: SettedUpDateTime.GetCurrentVietNamTime().AddMinutes(expiredTimeMinutes), // Thời gian hết hạn của token
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(secretKeyBytes),
                    SecurityAlgorithms.HmacSha512Signature
                )
            );
            var jwtTokenString = jwtTokenHandler.WriteToken(token);
            if (user.RefreshToken != null)
            {
                return new AuthenticationResponse
                {
                    AccessToken = jwtTokenString,
                    Role = user.Role?.Name,
                    RefreshToken = user.RefreshToken
                };
            }
            else
                throw new Exception();
        }

        public string GenerateJwtToken(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _config["JwtConfig:SecretKey"];
            var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            var issuer = _config["JwtConfig:Issuer"];
            int expiredTimeMinutes = _config.GetValue<int>("JwtConfig:ExpiredTimeMinutes");
            var ExpireTime = SettedUpDateTime
                .GetCurrentVietNamTime()
                .AddMinutes(expiredTimeMinutes);
            var claims = new List<Claim>
            {
                new Claim("Id", user.Id.ToString()),
                new Claim("TokenId", Guid.NewGuid().ToString()),
                new Claim("DeviceId", Guid.NewGuid().ToString()),
                new Claim("ExpireTime", ExpireTime.ToString()),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? ""),
            };
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: issuer,
                claims: claims,
                expires: SettedUpDateTime.GetCurrentVietNamTime().AddMinutes(expiredTimeMinutes), // Thời gian hết hạn của token
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(secretKeyBytes),
                    SecurityAlgorithms.HmacSha512Signature
                )
            );
            var jwtTokenString = jwtTokenHandler.WriteToken(token);
            if (jwtTokenString != null)
            {
                return jwtTokenString;
            }
            else
                throw new Exception();
        }

        public ClaimsPrincipal? GetClaimsPrincipal(string jwtToken)
        {
            var secretKey = _config["JwtConfig:SecretKey"];
            var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
                ValidateIssuer = false,
                ValidateAudience = false
            };

            try
            {
                var claimsPrincipal = tokenHandler.ValidateToken(
                    jwtToken,
                    validationParameters,
                    out var validatedToken
                );
                return claimsPrincipal;
            }
            catch
            {
                return null; // Trả về null trong trường hợp xảy ra lỗi.
            }
        }

        public string GenerateNewRefreshToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[32]; // 32 bytes for a secure 256-bit token
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes);
            }
        }

        public Guid GetUserIdByJwtToken(string jwtToken)
        {
            var decodedToken = GetClaimsPrincipal(jwtToken);
            if (decodedToken != null)
            {
                var userIdClaim = decodedToken.Claims.FirstOrDefault(c => c.Type == "Id");
                if (userIdClaim != null)
                    return Guid.Parse(userIdClaim.Value);
            }
            throw new Exception("Internal server error");
        }

        public string GetRoleNameByJwtToken(string jwtToken)
        {
            var decodedToken = GetClaimsPrincipal(jwtToken);
            if (decodedToken != null)
            {
                var userRoleClaim = decodedToken.Claims.FirstOrDefault(
                    c => c.Type == ClaimTypes.Role
                );
                if (userRoleClaim != null)
                    return userRoleClaim.Value;
            }
            throw new Exception("Internal server error");
        }
    }
}
