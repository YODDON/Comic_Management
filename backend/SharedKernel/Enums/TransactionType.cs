namespace SharedKernel.Enums
{
    public enum TransactionType
    {
        Purchase,
        ManualTopUp,
        Withdraw,
        Refund,

        /// <summary>
        /// Coins granted for free (e.g. mission rewards). Spendable on chapters but NOT withdrawable
        /// as cash — otherwise, at a 1:1 coin:VND rate, farming missions would drain real money.
        /// </summary>
        MissionReward,

        /// <summary>Withdrawal fee charged on top of a withdrawal (5%, capped at 10,000 coins).</summary>
        Fee
    }
}
