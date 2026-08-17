using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;

namespace WalletAPI.Application.Features.Currency.Commands
{
    public class CreateCurrencyEntryCommand : IRequest<ApiResponse<CurrencyEntryResultDto>>
    {
        public CreateCurrencyEntryRequestDto Request { get; set; } = null!;
    }

    public class CreateCurrencyEntryCommandHandler : IRequestHandler<CreateCurrencyEntryCommand, ApiResponse<CurrencyEntryResultDto>>
    {
        private readonly ICurrencyRepository _repository;

        public CreateCurrencyEntryCommandHandler(ICurrencyRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<CurrencyEntryResultDto>> Handle(CreateCurrencyEntryCommand request, CancellationToken cancellationToken)
        {
            if (request.Request.UserId <= 0)
                return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("UserId is required.", 400);

            if (request.Request.Amount == 0)
                return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("Amount must not be zero.", 400);

            if (!Enum.IsDefined(typeof(SharedKernel.Enums.TransactionType), request.Request.Type))
                return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("Transaction type is invalid.", 400);

            if (string.IsNullOrWhiteSpace(request.Request.Description))
                return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("Description is required.", 400);

            var (entry, balance, insufficientBalance) = await _repository.CreateEntryAsync(
                request.Request.UserId,
                request.Request.Amount,
                request.Request.Type,
                request.Request.Description);

            if (insufficientBalance)
                return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("Insufficient balance for debit.", 422);

            var result = new CurrencyEntryResultDto
            {
                Entry = new CurrencyEntryDto
                {
                    Id = entry!.Id,
                    Amount = entry.Amount,
                    Type = entry.Type,
                    Description = entry.Description,
                    ReferenceId = entry.ReferenceId,
                    CreatedAt = entry.CreatedAt
                },
                Balance = balance
            };

            return new ApiResponse<CurrencyEntryResultDto>(result, "Currency entry created successfully.", 201);
        }
    }
}
