using SharedKernel.Entities;

namespace WalletAPI.Entities;

public class Wallet : BaseEntity
{
    public int UserId { get; set; }
    public decimal Balance { get; set; }

    public ICollection<CurrencyEntry> CurrencyEntries { get; set; } = new List<CurrencyEntry>();
    public ICollection<Withdraw> Withdraws { get; set; } = new List<Withdraw>();
}
