using System.Security.Claims;
using UserAPI.Entities;

namespace UserAPI.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(User user, IList<string> roles);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
    }
}
