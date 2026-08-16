using SharedKernel.Responses;
using UserAPI.Application.DTOs;

namespace UserAPI.Application.Interfaces;

public interface IAdminUserService
{
    Task<ApiResponse<PagedResult<AdminUserDto>>> GetUsersAsync(string? search, bool? isActive, int pageNumber, int pageSize);
    Task<ApiResponse<AdminUserDto>> GetUserAsync(int id);
    Task<ApiResponse<AdminUserDto>> UpdateLockAsync(int id, int? currentUserId, UpdateUserLockDto request);
    Task<ApiResponse<AdminUserDto>> UpdateRoleAsync(int id, int? currentUserId, UpdateUserRoleDto request);
}
