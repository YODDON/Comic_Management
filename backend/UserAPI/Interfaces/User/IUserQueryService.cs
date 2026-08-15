using System.Security.Claims;
using UserAPI.Entities;

namespace UserAPI.Interfaces;

public interface IUserQueryService
{
    Task<User?> GetUserByIdAsync(int id);
    Task<List<User>> GetUsersByIdsAsync(IEnumerable<int> ids);
    ClaimsPrincipal? ValidateToken(string token);
}
