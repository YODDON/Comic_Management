using SharedKernel.Responses;
using UserAPI.Application.DTOs;
using UserAPI.Domain.Entities;
using UserAPI.Application.Interfaces; using UserAPI.Domain.Interfaces;

namespace UserAPI.Application.Services;

public class AdminUserService : IAdminUserService
{
    private static readonly string[] AssignableRoles = ["Reader", "Guest"];
    private readonly IAdminUserRepository _repository;

    public AdminUserService(IAdminUserRepository repository) => _repository = repository;

    public async Task<ApiResponse<PagedResult<AdminUserDto>>> GetUsersAsync(
        string? search, bool? isActive, int pageNumber, int pageSize)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await _repository.GetUsersAsync(search, isActive, pageNumber, pageSize);
        var result = new PagedResult<AdminUserDto>(items.Select(ToDto).ToList(), totalCount, pageNumber, pageSize);
        return new ApiResponse<PagedResult<AdminUserDto>>(result, "Users retrieved successfully.");
    }

    public async Task<ApiResponse<AdminUserDto>> GetUserAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        if (user is null) return ApiResponse<AdminUserDto>.ErrorResponse("Không tìm thấy người dùng.", 404);
        if (IsAdmin(user)) return ApiResponse<AdminUserDto>.ErrorResponse("Không thể quản lý tài khoản Admin.", 403);
        return new ApiResponse<AdminUserDto>(ToDto(user), "User retrieved successfully.");
    }

    public async Task<ApiResponse<AdminUserDto>> UpdateLockAsync(int id, int? currentUserId, UpdateUserLockDto request)
    {
        if (currentUserId == id)
            return ApiResponse<AdminUserDto>.ErrorResponse("Bạn không thể tự khóa tài khoản của mình.", 400);
        var user = await _repository.GetByIdAsync(id, trackChanges: true, includeRefreshTokens: true);
        if (user is null) return ApiResponse<AdminUserDto>.ErrorResponse("Không tìm thấy người dùng.", 404);
        if (IsAdmin(user)) return ApiResponse<AdminUserDto>.ErrorResponse("Không thể khóa tài khoản Admin.", 403);

        user.IsActive = !request.IsLocked;
        user.UpdatedAt = DateTime.UtcNow;
        if (request.IsLocked) RevokeRefreshTokens(user);
        await _repository.SaveChangesAsync();
        return new ApiResponse<AdminUserDto>(ToDto(user),
            request.IsLocked ? "Đã khóa tài khoản người dùng." : "Đã mở khóa tài khoản người dùng.");
    }

    public async Task<ApiResponse<AdminUserDto>> UpdateRoleAsync(int id, int? currentUserId, UpdateUserRoleDto request)
    {
        var normalizedRole = AssignableRoles.FirstOrDefault(role =>
            role.Equals(request.Role.Trim(), StringComparison.OrdinalIgnoreCase));
        if (normalizedRole is null)
            return ApiResponse<AdminUserDto>.ErrorResponse(
                "Admin chỉ được phân quyền Reader hoặc Guest. Không được gán role Admin.", 400);
        if (currentUserId == id)
            return ApiResponse<AdminUserDto>.ErrorResponse("Bạn không thể chỉnh role tài khoản Admin của mình.", 400);

        var user = await _repository.GetByIdAsync(id, trackChanges: true, includeRefreshTokens: true);
        if (user is null) return ApiResponse<AdminUserDto>.ErrorResponse("Không tìm thấy người dùng.", 404);
        if (IsAdmin(user)) return ApiResponse<AdminUserDto>.ErrorResponse("Không được chỉnh role của tài khoản Admin.", 403);

        var role = await _repository.GetRoleByNameAsync(normalizedRole);
        if (role is null)
            return ApiResponse<AdminUserDto>.ErrorResponse($"Role {normalizedRole} chưa tồn tại trong hệ thống.", 500);

        _repository.ReplaceRole(user, role);
        user.UpdatedAt = DateTime.UtcNow;
        RevokeRefreshTokens(user);
        await _repository.SaveChangesAsync();
        return new ApiResponse<AdminUserDto>(ToDto(user),
            $"Đã phân quyền {normalizedRole} cho người dùng. Người dùng cần đăng nhập lại.");
    }

    private static bool IsAdmin(User user) => user.UserRoles.Any(item =>
        item.Role.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));

    private static void RevokeRefreshTokens(User user)
    {
        foreach (var token in user.RefreshTokens.Where(token => !token.IsRevoked))
        {
            token.IsRevoked = true;
            token.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static AdminUserDto ToDto(User user) => new()
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
