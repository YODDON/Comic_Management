using SharedKernel.Responses;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;

namespace WalletAPI.Services;

public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository _repository;

    public CurrencyService(ICurrencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<ApiResponse<CurrencyHistoryResultDto>> GetHistoryAsync(
        int userId,
        int pageNumber,
        int pageSize)
    {
        if (pageNumber < 1 || pageSize is < 1 or > 100)
        {
            return ApiResponse<CurrencyHistoryResultDto>.ErrorResponse(
                "Page number must be at least 1 and page size must be between 1 and 100.",
                400);
        }

        var (items, totalCount, balance) = await _repository.GetHistoryAsync(userId, pageNumber, pageSize);
        var entryDtos = items.Select(x => new CurrencyEntryDto
        {
            Id = x.Id,
            Amount = x.Amount,
            Type = x.Type,
            Description = x.Description,
            ReferenceId = x.ReferenceId,
            CreatedAt = x.CreatedAt
        }).ToList();

        var result = new CurrencyHistoryResultDto(entryDtos, totalCount, pageNumber, pageSize, balance);
        return new ApiResponse<CurrencyHistoryResultDto>(result, "Currency history retrieved successfully.");
    }

    public async Task<ApiResponse<CurrencyEntryResultDto>> CreateEntryAsync(CreateCurrencyEntryRequestDto request)
    {
        if (request.UserId <= 0)
        {
            return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("UserId is required.", 400);
        }

        if (request.Amount == 0)
        {
            return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("Amount must not be zero.", 400);
        }

        if (!Enum.IsDefined(request.Type))
        {
            return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("Transaction type is invalid.", 400);
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("Description is required.", 400);
        }

        var (entry, balance, insufficientBalance) = await _repository.CreateEntryAsync(
            request.UserId,
            request.Amount,
            request.Type,
            request.Description);

        if (insufficientBalance)
        {
            return ApiResponse<CurrencyEntryResultDto>.ErrorResponse("Insufficient wallet balance.", 422);
        }

        var result = new CurrencyEntryResultDto
        {
            Balance = balance,
            Entry = new CurrencyEntryDto
            {
                Id = entry!.Id,
                Amount = entry.Amount,
                Type = entry.Type,
                Description = entry.Description,
                ReferenceId = entry.ReferenceId,
                CreatedAt = entry.CreatedAt
            }
        };

        return new ApiResponse<CurrencyEntryResultDto>(result, "Wallet balance updated successfully.", 201);
    }
}
