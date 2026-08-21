using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using UserAPI.Application.DTOs;
using UserAPI.Domain.Interfaces;

namespace UserAPI.Application.Features.Admin.Commands
{
    public class UpdateLockCommand : IRequest<ApiResponse<AdminUserDto>>
    {
        public int UserId { get; set; }
        public int? CurrentUserId { get; set; }
        public UpdateUserLockDto Request { get; set; } = null!;
    }

    public class UpdateLockCommandHandler : IRequestHandler<UpdateLockCommand, ApiResponse<AdminUserDto>>
    {
        private readonly IAdminUserRepository _repository;

        public UpdateLockCommandHandler(IAdminUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<AdminUserDto>> Handle(UpdateLockCommand request, CancellationToken cancellationToken)
        {
            if (request.CurrentUserId == request.UserId)
                return ApiResponse<AdminUserDto>.ErrorResponse("Bạn không thể tự khóa tài khoản của mình.", 400);
            var user = await _repository.GetByIdAsync(request.UserId, trackChanges: true, includeRefreshTokens: true);
            if (user is null) return ApiResponse<AdminUserDto>.ErrorResponse("Không tìm thấy người dùng.", 404);
            if (AdminHelper.IsAdmin(user)) return ApiResponse<AdminUserDto>.ErrorResponse("Không thể khóa tài khoản Admin.", 403);

            user.IsActive = !request.Request.IsLocked;
            user.UpdatedAt = DateTime.UtcNow;
            if (request.Request.IsLocked) AdminHelper.RevokeRefreshTokens(user);
            await _repository.SaveChangesAsync();
            return new ApiResponse<AdminUserDto>(AdminHelper.ToDto(user),
                request.Request.IsLocked ? "Đã khóa tài khoản người dùng." : "Đã mở khóa tài khoản người dùng.");
        }
    }
}
