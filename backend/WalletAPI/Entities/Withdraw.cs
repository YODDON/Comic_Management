using SharedKernel.Entities;
using SharedKernel.Enums;

namespace WalletAPI.Entities;

public class Withdraw : BaseEntity
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public string BankAccount { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    /// <summary>Account holder name, for the admin to verify before transferring.</summary>
    public string AccountName { get; set; } = string.Empty;
    public WithdrawStatus Status { get; set; } = WithdrawStatus.Pending;
    public string? Note { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public Wallet Wallet { get; set; } = null!;
}
