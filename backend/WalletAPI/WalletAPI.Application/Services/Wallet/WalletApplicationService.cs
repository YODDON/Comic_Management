using WalletAPI.DTOs;
using WalletAPI.Interfaces;

namespace WalletAPI.Services;

public class WalletApplicationService : IWalletApplicationService
{
    private readonly IWalletRepository _repository;

    public WalletApplicationService(IWalletRepository repository) => _repository = repository;

    public Task<WalletCreditResultDto> AddCoinAsync(
        int userId, decimal amount, Guid referenceId, string? creditType, string? description) =>
        _repository.CreditAsync(
            userId,
            amount,
            referenceId,
            WalletRules.ResolveCreditType(creditType),
            string.IsNullOrWhiteSpace(description) ? "Coin credit" : description);

    public Task<WalletDebitResultDto> DebitCoinAsync(
        int userId, decimal amount, Guid referenceId, string? description) =>
        _repository.DebitAsync(
            userId,
            amount,
            referenceId,
            string.IsNullOrWhiteSpace(description) ? "Chapter purchase" : description);
}
