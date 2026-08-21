using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using UserAPI.Application.DTOs;
using UserAPI.Domain.Interfaces;

namespace UserAPI.Application.Features.Admin.Queries
{
    public class GetUserQuery : IRequest<ApiResponse<AdminUserDto>>
    {
        public int Id { get; set; }
    }

    public class GetUserQueryHandler : IRequestHandler<GetUserQuery, ApiResponse<AdminUserDto>>
    {
        private readonly IAdminUserRepository _repository;

        public GetUserQueryHandler(IAdminUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<AdminUserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _repository.GetByIdAsync(request.Id);
            if (user is null) return ApiResponse<AdminUserDto>.ErrorResponse("Không tìm thấy người dùng.", 404);
            if (AdminHelper.IsAdmin(user)) return ApiResponse<AdminUserDto>.ErrorResponse("Không thể quản lý tài khoản Admin.", 403);
            return new ApiResponse<AdminUserDto>(AdminHelper.ToDto(user), "User retrieved successfully.");
        }
    }
}
