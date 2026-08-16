using System.Security.Claims;
using UserAPI.Domain.Entities;

namespace UserAPI.Application.Interfaces;

public interface IUserQueryService
{
    Task<User?> GetUserByIdAsync(int id);
    Task<List<User>> GetUsersByIdsAsync(IEnumerable<int> ids);
    ClaimsPrincipal? ValidateToken(string token);
}
