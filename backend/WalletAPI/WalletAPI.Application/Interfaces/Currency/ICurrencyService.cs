using SharedKernel.Responses;
using WalletAPI.DTOs;

namespace WalletAPI.Interfaces;

public interface ICurrencyService
{
    Task<ApiResponse<CurrencyHistoryResultDto>> GetHistoryAsync(int userId, int pageNumber, int pageSize);
    Task<ApiResponse<CurrencyEntryResultDto>> CreateEntryAsync(CreateCurrencyEntryRequestDto request);
}
