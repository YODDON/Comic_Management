using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;
using WalletAPI.Services;

namespace WalletAPI.Application.Features.Withdraw.Commands
{
    public class CreateWithdrawCommand : IRequest<ApiResponse<WithdrawDto>>
    {
        public int UserId { get; set; }
        public CreateWithdrawRequestDto Request { get; set; }
    }

    public class CreateWithdrawCommandHandler : IRequestHandler<CreateWithdrawCommand, ApiResponse<WithdrawDto>>
    {
        private readonly IWithdrawRepository _repository;

        public CreateWithdrawCommandHandler(IWithdrawRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<WithdrawDto>> Handle(CreateWithdrawCommand request, CancellationToken cancellationToken)
        {
            if (request.Request.Amount <= 0)
            {
                return ApiResponse<WithdrawDto>.ErrorResponse("Amount must be greater than zero.", 400);
            }

            if (string.IsNullOrWhiteSpace(request.Request.BankAccount) || string.IsNullOrWhiteSpace(request.Request.BankName))
            {
                return ApiResponse<WithdrawDto>.ErrorResponse("Bank account and bank name are required.", 400);
            }

            var (withdraw, balance, pendingExists, insufficientBalance) = await _repository.CreateAsync(
                request.UserId,
                request.Request.Amount,
                request.Request.BankAccount,
                request.Request.BankName,
                request.Request.AccountName);

            if (pendingExists)
            {
                return ApiResponse<WithdrawDto>.ErrorResponse("A pending withdrawal request already exists.", 409);
            }

            if (insufficientBalance)
            {
                return ApiResponse<WithdrawDto>.ErrorResponse("Insufficient wallet balance.", 422);
            }

            var result = new WithdrawDto
            {
                Id = withdraw!.Id,
                UserId = request.UserId,
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

            return new ApiResponse<WithdrawDto>(result, "Withdrawal request created successfully.", 201);
        }
    }
}
