using Microsoft.EntityFrameworkCore;
using UserAPI.Infrastructure.Data;
using UserAPI.Domain.Entities;
using UserAPI.Application.Interfaces; using UserAPI.Domain.Interfaces;

namespace UserAPI.Infrastructure.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly UserDbContext _context;

    public AdminUserRepository(UserDbContext context) => _context = context;

    public async Task<(List<User> Items, int TotalCount)> GetUsersAsync(
        string? search, bool? isActive, int pageNumber, int pageSize)
    {
        var query = _context.Users.AsNoTracking()
            .Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role)
            .Where(user => !user.UserRoles.Any(userRole => userRole.Role.Name == "Admin"));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(user => user.Username.ToLower().Contains(keyword) || user.Email.ToLower().Contains(keyword));
        }

        if (isActive.HasValue) query = query.Where(user => user.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(user => user.CreatedAt)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public Task<User?> GetByIdAsync(int id, bool trackChanges = false, bool includeRefreshTokens = false)
    {
        IQueryable<User> query = _context.Users;
        if (!trackChanges) query = query.AsNoTracking();
        query = query.Include(user => user.UserRoles).ThenInclude(userRole => userRole.Role);
        if (includeRefreshTokens) query = query.Include(user => user.RefreshTokens);
        return query.SingleOrDefaultAsync(user => user.Id == id);
    }

    public Task<Role?> GetRoleByNameAsync(string roleName) =>
        _context.Roles.SingleOrDefaultAsync(role => role.Name == roleName);

    public void ReplaceRole(User user, Role role)
    {
        _context.UserRoles.RemoveRange(user.UserRoles);
        user.UserRoles.Clear();
        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            Role = role,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
