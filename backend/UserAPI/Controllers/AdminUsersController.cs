using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Responses;
using UserAPI.Data;
using UserAPI.DTOs;

namespace UserAPI.Controllers;

[ApiController]
[Route("auth/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private static readonly string[] AssignableRoles = ["Reader", "Guest"];
    private readonly UserDbContext _context;

    public AdminUsersController(UserDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Users
            .AsNoTracking()
            .Include(user => user.UserRoles)
            .ThenInclude(userRole => userRole.Role)
            .Where(user => !user.UserRoles.Any(userRole => userRole.Role.Name == "Admin"));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(user =>
                user.Username.ToLower().Contains(keyword) ||
                user.Email.ToLower().Contains(keyword));
        }

        if (isActive.HasValue)
        {
            query = query.Where(user => user.IsActive == isActive.Value);
        }

        var total = await query.CountAsync();
        var entities = await query
            .OrderByDescending(user => user.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new ApiResponse<PagedResult<AdminUserDto>>(
            new PagedResult<AdminUserDto>(
                entities.Select(ToDto).ToList(),
                total,
                pageNumber,
                pageSize),
            "Users retrieved successfully."));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (user is null)
        {
            return NotFound(ApiResponse<AdminUserDto>.ErrorResponse("Không tìm thấy người dùng.", 404));
        }

        if (IsAdmin(user))
        {
            return StatusCode(
                403,
                ApiResponse<AdminUserDto>.ErrorResponse("Không thể quản lý tài khoản Admin.", 403));
        }

        return Ok(new ApiResponse<AdminUserDto>(ToDto(user), "User retrieved successfully."));
    }

    [HttpPut("{id:int}/lock")]
    public async Task<IActionResult> UpdateLock(int id, [FromBody] UpdateUserLockDto request)
    {
        if (GetCurrentUserId() == id)
        {
            return BadRequest(ApiResponse<AdminUserDto>.ErrorResponse(
                "Bạn không thể tự khóa tài khoản của mình."));
        }

        var user = await FindUserForUpdate(id);
        if (user is null)
        {
            return NotFound(ApiResponse<AdminUserDto>.ErrorResponse("Không tìm thấy người dùng.", 404));
        }

        if (IsAdmin(user))
        {
            return StatusCode(
                403,
                ApiResponse<AdminUserDto>.ErrorResponse("Không thể khóa tài khoản Admin.", 403));
        }

        user.IsActive = !request.IsLocked;
        user.UpdatedAt = DateTime.UtcNow;

        if (request.IsLocked)
        {
            RevokeRefreshTokens(user);
        }

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<AdminUserDto>(
            ToDto(user),
            request.IsLocked
                ? "Đã khóa tài khoản người dùng."
                : "Đã mở khóa tài khoản người dùng."));
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleDto request)
    {
        var normalizedRole = AssignableRoles.FirstOrDefault(role =>
            role.Equals(request.Role.Trim(), StringComparison.OrdinalIgnoreCase));

        if (normalizedRole is null)
        {
            return BadRequest(ApiResponse<AdminUserDto>.ErrorResponse(
                "Admin chỉ được phân quyền Reader hoặc Guest. Không được gán role Admin."));
        }

        if (GetCurrentUserId() == id)
        {
            return BadRequest(ApiResponse<AdminUserDto>.ErrorResponse(
                "Bạn không thể chỉnh role tài khoản Admin của mình."));
        }

        var user = await FindUserForUpdate(id);
        if (user is null)
        {
            return NotFound(ApiResponse<AdminUserDto>.ErrorResponse("Không tìm thấy người dùng.", 404));
        }

        if (IsAdmin(user))
        {
            return StatusCode(
                403,
                ApiResponse<AdminUserDto>.ErrorResponse(
                    "Không được chỉnh role của tài khoản Admin.",
                    403));
        }

        var role = await _context.Roles.SingleOrDefaultAsync(item => item.Name == normalizedRole);
        if (role is null)
        {
            return StatusCode(
                500,
                ApiResponse<AdminUserDto>.ErrorResponse(
                    $"Role {normalizedRole} chưa tồn tại trong hệ thống.",
                    500));
        }

        _context.UserRoles.RemoveRange(user.UserRoles);
        user.UserRoles.Clear();
        user.UserRoles.Add(new Entities.UserRole
        {
            UserId = user.Id,
            RoleId = role.Id,
            Role = role,
            CreatedAt = DateTime.UtcNow
        });
        user.UpdatedAt = DateTime.UtcNow;
        RevokeRefreshTokens(user);

        await _context.SaveChangesAsync();

        return Ok(new ApiResponse<AdminUserDto>(
            ToDto(user),
            $"Đã phân quyền {normalizedRole} cho người dùng. Người dùng cần đăng nhập lại."));
    }

    private Task<Entities.User?> FindUserForUpdate(int id)
    {
        return _context.Users
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .Include(item => item.RefreshTokens)
            .SingleOrDefaultAsync(item => item.Id == id);
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;
    }

    private static bool IsAdmin(Entities.User user)
    {
        return user.UserRoles.Any(item =>
            item.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));
    }

    private static void RevokeRefreshTokens(Entities.User user)
    {
        foreach (var token in user.RefreshTokens.Where(token => !token.IsRevoked))
        {
            token.IsRevoked = true;
            token.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static AdminUserDto ToDto(Entities.User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        AvatarUrl = user.AvatarUrl,
        IsEmailVerified = user.IsEmailVerified,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        Roles = user.UserRoles.Select(item => item.Role.Name).ToList()
    };
}
