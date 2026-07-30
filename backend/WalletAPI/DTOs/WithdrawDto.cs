using SharedKernel.Enums;

namespace WalletAPI.DTOs;

public class WithdrawDto
{
    public Guid Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Amount the user receives (does not include the fee).</summary>
    public decimal Amount { get; set; }

    /// <summary>Fee charged on top: 5% of Amount, capped at 10,000.</summary>
    public decimal Fee { get; set; }

    /// <summary>Total deducted from the wallet = Amount + Fee.</summary>
    public decimal TotalDeducted { get; set; }

    public string BankAccount { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public WithdrawStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public decimal? Balance { get; set; }
}
