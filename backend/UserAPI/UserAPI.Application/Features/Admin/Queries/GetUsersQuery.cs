using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using UserAPI.Application.DTOs;
using UserAPI.Domain.Interfaces;

namespace UserAPI.Application.Features.Admin.Queries
{
    public class GetUsersQuery : IRequest<ApiResponse<PagedResult<AdminUserDto>>>
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, ApiResponse<PagedResult<AdminUserDto>>>
    {
        private readonly IAdminUserRepository _repository;

        public GetUsersQueryHandler(IAdminUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<PagedResult<AdminUserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var pageNumber = Math.Max(request.PageNumber, 1);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);
            var (items, totalCount) = await _repository.GetUsersAsync(request.Search, request.IsActive, pageNumber, pageSize);
            var result = new PagedResult<AdminUserDto>(items.Select(AdminHelper.ToDto).ToList(), totalCount, pageNumber, pageSize);
            return new ApiResponse<PagedResult<AdminUserDto>>(result, "Users retrieved successfully.");
        }
    }
}
