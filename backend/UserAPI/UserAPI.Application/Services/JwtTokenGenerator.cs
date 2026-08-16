using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UserAPI.Domain.Entities;
using UserAPI.Application.Interfaces; using UserAPI.Domain.Interfaces;

namespace UserAPI.Application.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(User user, IList<string> roles)
        {
            var secretKey = Environment.GetEnvironmentVariable("JwtSettings__Secret") ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? _configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secretKey)) secretKey = "SuperSecretKeyForDevelopmentOnly123!AndItNeedsToBeAtLeast32BytesLong!";

            var issuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer") ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _configuration["JwtSettings:Issuer"];
            if (string.IsNullOrEmpty(issuer)) issuer = "prn232_comic_api";

            var audience = Environment.GetEnvironmentVariable("JwtSettings__Audience") ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? _configuration["JwtSettings:Audience"];
            if (string.IsNullOrEmpty(audience)) audience = "prn232_comic_api";

            var expiryMinutesStr = Environment.GetEnvironmentVariable("JwtSettings__ExpiryMinutes") ?? Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? _configuration["JwtSettings:ExpiryMinutes"];
            if (string.IsNullOrEmpty(expiryMinutesStr)) expiryMinutesStr = "60";
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(expiryMinutesStr)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var secretKey = Environment.GetEnvironmentVariable("JwtSettings__Secret") ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? _configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secretKey)) secretKey = "SuperSecretKeyForDevelopmentOnly123!AndItNeedsToBeAtLeast32BytesLong!";

            var issuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer") ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _configuration["JwtSettings:Issuer"];
            if (string.IsNullOrEmpty(issuer)) issuer = "prn232_comic_api";

            var audience = Environment.GetEnvironmentVariable("JwtSettings__Audience") ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? _configuration["JwtSettings:Audience"];
            if (string.IsNullOrEmpty(audience)) audience = "prn232_comic_api";

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, "jwt");
                return new ClaimsPrincipal(claimsIdentity);
            }
            catch
            {
                return null;
            }
        }
    }
}
