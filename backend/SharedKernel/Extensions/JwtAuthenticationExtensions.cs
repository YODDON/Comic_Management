using System;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace SharedKernel.Extensions
{
    public static class JwtAuthenticationExtensions
    {
        public static AuthenticationBuilder AddCustomJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var secretKey = Environment.GetEnvironmentVariable("JwtSettings__Secret") ?? Environment.GetEnvironmentVariable("JWT_SECRET") ?? configuration["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secretKey)) secretKey = "SuperSecretKeyForDevelopmentOnly123!AndItNeedsToBeAtLeast32BytesLong!";

            var issuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer") ?? Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["JwtSettings:Issuer"];
            if (string.IsNullOrEmpty(issuer)) issuer = "prn232_comic_api";

            var audience = Environment.GetEnvironmentVariable("JwtSettings__Audience") ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["JwtSettings:Audience"];
            if (string.IsNullOrEmpty(audience)) audience = "prn232_comic_api";

            return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                    };
                });
        }
    }
}
