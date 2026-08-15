using WalletAPI.Entities;

namespace WalletAPI.Interfaces;

public interface ICurrencyRepository
{
    Task<(List<CurrencyEntry> Items, int TotalCount, decimal Balance)> GetHistoryAsync(
        int userId,
        int pageNumber,
        int pageSize);

    Task<(CurrencyEntry? Entry, decimal Balance, bool InsufficientBalance)> CreateEntryAsync(
        int userId,
        decimal amount,
        SharedKernel.Enums.TransactionType type,
        string description);
}
