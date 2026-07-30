using WalletAPI.Entities;
using SharedKernel.Enums;

namespace WalletAPI.Interfaces;

public interface IWithdrawRepository
{
    Task<(Withdraw? Withdraw, decimal Balance, bool PendingExists, bool InsufficientBalance)> CreateAsync(
        int userId,
        decimal amount,
        string bankAccount,
        string bankName,
        string accountName);

    Task<(decimal Balance, decimal Withdrawable)> GetWithdrawableAsync(int userId);

    Task<(List<Withdraw> Items, int TotalCount)> GetMineAsync(
        int userId,
        int pageNumber,
        int pageSize);

    Task<(List<Withdraw> Items, int TotalCount)> GetAdminAsync(
        WithdrawStatus? status,
        string? search,
        int pageNumber,
        int pageSize);

    Task<(Withdraw? Withdraw, decimal Balance, bool NotFound, bool AlreadyProcessed)> UpdateStatusAsync(
        Guid id,
        WithdrawStatus status,
        string? note);
}
