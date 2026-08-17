using System;
using SharedKernel.Enums;

namespace WalletAPI.Services;

/// <summary>
/// Pure wallet rules, kept out of the DB layer so they can be unit-tested directly.
/// </summary>
public static class WalletRules
{
    /// <summary>
    /// Maps an AddCoin request's creditType to a ledger type. Fail-closed: only the exact string
    /// "ManualTopUp" is real, withdrawable money; anything else — empty, unknown, or a caller that
    /// predates this field — is treated as MissionReward and cannot be withdrawn. A credit becomes
    /// cashable only by explicitly declaring itself real money.
    /// </summary>
    public static TransactionType ResolveCreditType(string? creditType) =>
        creditType == TransactionType.ManualTopUp.ToString()
            ? TransactionType.ManualTopUp
            : TransactionType.MissionReward;

    /// <summary>
    /// How much of the balance may be withdrawn as cash. Free mission coins are excluded — they are
    /// spendable on chapters but never cashable, otherwise farming missions would drain real money
    /// at the 1:1 rate.
    /// </summary>
    /// <remarks>
    /// Mission coins are treated as spent FIRST, which favours the user: buying chapters consumes the
    /// free coins before touching real top-ups, so a user's own money stays withdrawable as long as
    /// possible.
    ///   missionRemaining = max(0, totalMissionRewardCredited - totalSpentOnPurchases)
    ///   withdrawable      = max(0, balance - missionRemaining)
    /// </remarks>
    public static decimal Withdrawable(decimal balance, decimal missionRewardCredited, decimal purchaseSpend)
    {
        var missionRemaining = Math.Max(0m, missionRewardCredited - purchaseSpend);
        return Math.Max(0m, balance - missionRemaining);
    }

    /// <summary>Withdrawal fee rate: 5% of the requested amount.</summary>
    public const decimal WithdrawFeeRate = 0.05m;

    /// <summary>Maximum fee, regardless of amount: 10,000 coins.</summary>
    public const decimal WithdrawFeeCap = 10_000m;

    /// <summary>
    /// The fee charged on a withdrawal: 5% of the amount, capped at 10,000 coins. Charged ON TOP of
    /// the amount — the user receives the full requested amount and the fee is an extra deduction.
    /// </summary>
    public static decimal WithdrawFee(decimal amount)
    {
        if (amount <= 0)
        {
            return 0m;
        }

        return Math.Min(amount * WithdrawFeeRate, WithdrawFeeCap);
    }
}
