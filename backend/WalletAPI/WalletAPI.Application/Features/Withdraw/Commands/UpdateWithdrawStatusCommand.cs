using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Enums;
using SharedKernel.Responses;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;
using WalletAPI.Services;

namespace WalletAPI.Application.Features.Withdraw.Commands
{
    public class UpdateWithdrawStatusCommand : IRequest<ApiResponse<WithdrawDto>>
    {
        public Guid Id { get; set; }
        public UpdateWithdrawStatusRequestDto Request { get; set; }
    }

    public class UpdateWithdrawStatusCommandHandler : IRequestHandler<UpdateWithdrawStatusCommand, ApiResponse<WithdrawDto>>
    {
        private readonly IWithdrawRepository _repository;

        public UpdateWithdrawStatusCommandHandler(IWithdrawRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<WithdrawDto>> Handle(UpdateWithdrawStatusCommand request, CancellationToken cancellationToken)
        {
            if (request.Id == Guid.Empty)
                return ApiResponse<WithdrawDto>.ErrorResponse("Withdraw id is invalid.", 400);

            if (request.Request.Status is not (WithdrawStatus.Approved or WithdrawStatus.Rejected))
                return ApiResponse<WithdrawDto>.ErrorResponse("Status must be Approved or Rejected.", 400);

            var (withdraw, balance, notFound, alreadyProcessed) = await _repository.UpdateStatusAsync(
                request.Id,
                request.Request.Status,
                request.Request.Note);

            if (notFound)
                return ApiResponse<WithdrawDto>.ErrorResponse("Withdrawal request not found.", 404);

            if (alreadyProcessed)
                return ApiResponse<WithdrawDto>.ErrorResponse("Withdrawal request has already been processed.", 409);

            var result = new WithdrawDto
            {
                Id = withdraw!.Id,
                UserId = withdraw.Wallet.UserId,
                Amount = withdraw.Amount,
                Fee = WalletRules.WithdrawFee(withdraw.Amount),
                TotalDeducted = withdraw.Amount + WalletRules.WithdrawFee(withdraw.Amount),
                BankAccount = withdraw.BankAccount,
                BankName = withdraw.BankName,
                AccountName = withdraw.AccountName,
                Status = withdraw.Status,
                Note = withdraw.Note,
                CreatedAt = withdraw.CreatedAt,
                ProcessedAt = withdraw.ProcessedAt,
                Balance = balance
            };

            var message = request.Request.Status == WithdrawStatus.Approved
                ? "Withdrawal approved. External transfer recorded."
                : "Withdrawal rejected and balance refunded.";
            return new ApiResponse<WithdrawDto>(result, message);
        }
    }
}
