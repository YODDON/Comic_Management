namespace WalletAPI.DTOs;

public class CurrencyEntryResultDto
{
    public CurrencyEntryDto Entry { get; set; } = null!;
    public decimal Balance { get; set; }
}
