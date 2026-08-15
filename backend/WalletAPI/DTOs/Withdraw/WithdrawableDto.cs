namespace WalletAPI.DTOs;

/// <summary>
/// What the withdraw form needs: how much of the balance is actually cashable.
/// </summary>
public class WithdrawableDto
{
    /// <summary>Total wallet balance (includes non-withdrawable mission coins).</summary>
    public decimal Balance { get; set; }

    /// <summary>The amount that may be withdrawn (balance minus locked mission coins).</summary>
    public decimal Withdrawable { get; set; }

    /// <summary>Balance that is NOT withdrawable (unspent mission-reward coins).</summary>
    public decimal Locked { get; set; }
}
