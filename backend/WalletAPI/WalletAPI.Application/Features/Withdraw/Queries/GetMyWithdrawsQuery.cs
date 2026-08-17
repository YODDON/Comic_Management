using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;
using WalletAPI.Services;

namespace WalletAPI.Application.Features.Withdraw.Queries
{
    public class GetMyWithdrawsQuery : IRequest<ApiResponse<PagedResult<WithdrawDto>>>
    {
        public int UserId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class GetMyWithdrawsQueryHandler : IRequestHandler<GetMyWithdrawsQuery, ApiResponse<PagedResult<WithdrawDto>>>
    {
        private readonly IWithdrawRepository _repository;

        public GetMyWithdrawsQueryHandler(IWithdrawRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<PagedResult<WithdrawDto>>> Handle(GetMyWithdrawsQuery request, CancellationToken cancellationToken)
        {
            if (request.PageNumber < 1 || request.PageSize is < 1 or > 100)
                return ApiResponse<PagedResult<WithdrawDto>>.ErrorResponse("Page number must be at least 1 and page size must be between 1 and 100.", 400);

            var (items, totalCount) = await _repository.GetMineAsync(request.UserId, request.PageNumber, request.PageSize);
            
            var dtos = items.Select(x => new WithdrawDto
            {
                Id = x.Id,
                UserId = x.Wallet.UserId,
                Amount = x.Amount,
                Fee = WalletRules.WithdrawFee(x.Amount),
                TotalDeducted = x.Amount + WalletRules.WithdrawFee(x.Amount),
                BankAccount = x.BankAccount,
                BankName = x.BankName,
                AccountName = x.AccountName,
                Status = x.Status,
                Note = x.Note,
                CreatedAt = x.CreatedAt,
                ProcessedAt = x.ProcessedAt
            }).ToList();

            var result = new PagedResult<WithdrawDto>(dtos, totalCount, request.PageNumber, request.PageSize);
            return new ApiResponse<PagedResult<WithdrawDto>>(result, "Withdrawal requests retrieved successfully.");
        }
    }
}
