using SharedKernel.Responses;

namespace WalletAPI.DTOs;

public class CurrencyHistoryResultDto : PagedResult<CurrencyEntryDto>
{
    public decimal Balance { get; set; }

    public CurrencyHistoryResultDto(
        List<CurrencyEntryDto> items,
        int totalCount,
        int page,
        int pageSize,
        decimal balance) : base(items, totalCount, page, pageSize)
    {
        Balance = balance;
    }
}
