using SharedKernel.Enums;
using WalletAPI.DTOs;

namespace WalletAPI.Interfaces;

public interface IWalletRepository
{
    Task<WalletCreditResultDto> CreditAsync(
        int userId, decimal amount, Guid referenceId, TransactionType type, string description);
    Task<WalletDebitResultDto> DebitAsync(
        int userId, decimal amount, Guid referenceId, string description);
}
