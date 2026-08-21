using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using UserAPI.Application.DTOs;
using UserAPI.Domain.Interfaces;

namespace UserAPI.Application.Features.Admin.Commands
{
    public class UpdateRoleCommand : IRequest<ApiResponse<AdminUserDto>>
    {
        public int UserId { get; set; }
        public int? CurrentUserId { get; set; }
        public UpdateUserRoleDto Request { get; set; } = null!;
    }

    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, ApiResponse<AdminUserDto>>
    {
        private readonly IAdminUserRepository _repository;

        public UpdateRoleCommandHandler(IAdminUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<AdminUserDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var normalizedRole = AdminHelper.AssignableRoles.FirstOrDefault(role =>
                role.Equals(request.Request.Role.Trim(), StringComparison.OrdinalIgnoreCase));
            if (normalizedRole is null)
                return ApiResponse<AdminUserDto>.ErrorResponse(
                    "Admin chỉ được phân quyền Reader hoặc Guest. Không được gán role Admin.", 400);
            if (request.CurrentUserId == request.UserId)
                return ApiResponse<AdminUserDto>.ErrorResponse("Bạn không thể chỉnh role tài khoản Admin của mình.", 400);

            var user = await _repository.GetByIdAsync(request.UserId, trackChanges: true, includeRefreshTokens: true);
            if (user is null) return ApiResponse<AdminUserDto>.ErrorResponse("Không tìm thấy người dùng.", 404);
            if (AdminHelper.IsAdmin(user)) return ApiResponse<AdminUserDto>.ErrorResponse("Không được chỉnh role của tài khoản Admin.", 403);

            var role = await _repository.GetRoleByNameAsync(normalizedRole);
            if (role is null)
                return ApiResponse<AdminUserDto>.ErrorResponse($"Role {normalizedRole} chưa tồn tại trong hệ thống.", 500);

            _repository.ReplaceRole(user, role);
            user.UpdatedAt = DateTime.UtcNow;
            AdminHelper.RevokeRefreshTokens(user);
            await _repository.SaveChangesAsync();
            return new ApiResponse<AdminUserDto>(AdminHelper.ToDto(user),
                $"Đã phân quyền {normalizedRole} cho người dùng. Người dùng cần đăng nhập lại.");
        }
    }
}
