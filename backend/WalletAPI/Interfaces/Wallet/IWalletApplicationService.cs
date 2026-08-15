using WalletAPI.DTOs;

namespace WalletAPI.Interfaces;

public interface IWalletApplicationService
{
    Task<WalletCreditResultDto> AddCoinAsync(
        int userId, decimal amount, Guid referenceId, string? creditType, string? description);
    Task<WalletDebitResultDto> DebitCoinAsync(
        int userId, decimal amount, Guid referenceId, string? description);
}
