using UserAPI.Entities;

namespace UserAPI.Interfaces;

public interface IAdminUserRepository
{
    Task<(List<User> Items, int TotalCount)> GetUsersAsync(string? search, bool? isActive, int pageNumber, int pageSize);
    Task<User?> GetByIdAsync(int id, bool trackChanges = false, bool includeRefreshTokens = false);
    Task<Role?> GetRoleByNameAsync(string roleName);
    void ReplaceRole(User user, Role role);
    Task SaveChangesAsync();
}
