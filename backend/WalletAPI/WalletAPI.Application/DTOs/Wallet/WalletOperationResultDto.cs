namespace WalletAPI.DTOs;

public record WalletCreditResultDto(bool AlreadyProcessed, decimal Balance);

public record WalletDebitResultDto(
    bool Success,
    bool AlreadyProcessed,
    bool InsufficientBalance,
    decimal Balance);
