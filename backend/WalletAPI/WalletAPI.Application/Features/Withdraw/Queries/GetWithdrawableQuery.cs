using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;

namespace WalletAPI.Application.Features.Withdraw.Queries
{
    public class GetWithdrawableQuery : IRequest<ApiResponse<WithdrawableDto>>
    {
        public int UserId { get; set; }
    }

    public class GetWithdrawableQueryHandler : IRequestHandler<GetWithdrawableQuery, ApiResponse<WithdrawableDto>>
    {
        private readonly IWithdrawRepository _repository;

        public GetWithdrawableQueryHandler(IWithdrawRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<WithdrawableDto>> Handle(GetWithdrawableQuery request, CancellationToken cancellationToken)
        {
            var (balance, withdrawable) = await _repository.GetWithdrawableAsync(request.UserId);
            return new ApiResponse<WithdrawableDto>(new WithdrawableDto
            {
                Balance = balance,
                Withdrawable = withdrawable,
                Locked = balance - withdrawable
            });
        }
    }
}
