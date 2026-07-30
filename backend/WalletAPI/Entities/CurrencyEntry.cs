using SharedKernel.Entities;
using SharedKernel.Enums;

namespace WalletAPI.Entities;

public class CurrencyEntry : BaseEntity
{
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }

    public Wallet Wallet { get; set; } = null!;
}
