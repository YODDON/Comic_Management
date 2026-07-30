using System.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Enums;
using WalletAPI.Data;
using WalletAPI.Entities;
using WalletAPI.Interfaces;

namespace WalletAPI.Repositories;

public class WithdrawRepository : IWithdrawRepository
{
    private readonly WalletDbContext _context;

    public WithdrawRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<(Withdraw? Withdraw, decimal Balance, bool PendingExists, bool InsufficientBalance)> CreateAsync(
        int userId,
        decimal amount,
        string bankAccount,
        string bankName,
        string accountName)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var wallet = await _context.Wallets.SingleOrDefaultAsync(x => x.UserId == userId);
        if (wallet is null)
        {
            return (null, 0m, false, true);
        }

        var pendingExists = await _context.Withdraws.AnyAsync(
            x => x.WalletId == wallet.Id && x.Status == WithdrawStatus.Pending);
        if (pendingExists)
        {
            return (null, wallet.Balance, true, false);
        }

        // CurrencyEntry is the source of truth required by the backlog.
        var (ledgerBalance, withdrawable) = await ComputeBalancesAsync(wallet.Id);

        // The fee is charged ON TOP of the amount: the user receives the full requested amount and
        // must have amount + fee available. Reject if they cannot cover both.
        var fee = Services.WalletRules.WithdrawFee(amount);
        var totalDeducted = amount + fee;

        if (totalDeducted > withdrawable)
        {
            // Report the withdrawable figure, not the raw balance — the raw balance may include
            // mission coins the user cannot actually withdraw.
            return (null, withdrawable, false, true);
        }

        var withdraw = new Withdraw
        {
            WalletId = wallet.Id,
            Amount = amount,
            BankAccount = bankAccount.Trim(),
            BankName = bankName.Trim(),
            AccountName = accountName.Trim(),
            Status = WithdrawStatus.Pending
        };

        wallet.Balance = ledgerBalance - totalDeducted;
        wallet.UpdatedAt = DateTime.UtcNow;

        _context.Withdraws.Add(withdraw);
        _context.CurrencyEntries.Add(new CurrencyEntry
        {
            WalletId = wallet.Id,
            Amount = -amount,
            Type = TransactionType.Withdraw,
            Description = $"Pending withdrawal to {withdraw.BankName} - {withdraw.BankAccount}",
            ReferenceId = withdraw.Id
        });

        if (fee > 0)
        {
            // ReferenceId is null (not withdraw.Id): CurrencyEntry.ReferenceId is unique-indexed, and
            // the withdrawal entry above already claims withdraw.Id. The fee is part of the same
            // committed transaction, so it needs no idempotency key of its own; the description links it.
            _context.CurrencyEntries.Add(new CurrencyEntry
            {
                WalletId = wallet.Id,
                Amount = -fee,
                Type = TransactionType.Fee,
                Description = $"Withdrawal fee (5%, cap {Services.WalletRules.WithdrawFeeCap:N0}) for {withdraw.Id}",
                ReferenceId = null
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (withdraw, wallet.Balance, false, false);
    }

    public async Task<(decimal Balance, decimal Withdrawable)> GetWithdrawableAsync(int userId)
    {
        var wallet = await _context.Wallets.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId);
        if (wallet is null)
        {
            return (0m, 0m);
        }

        return await ComputeBalancesAsync(wallet.Id);
    }

    /// <summary>
    /// Ledger balance and the withdrawable portion of it (excluding unspent mission coins).
    /// Single source of truth for both the withdraw check and the withdrawable endpoint.
    /// </summary>
    private async Task<(decimal Balance, decimal Withdrawable)> ComputeBalancesAsync(Guid walletId)
    {
        var entries = _context.CurrencyEntries.Where(x => x.WalletId == walletId);

        var balance = await entries.SumAsync(x => (decimal?)x.Amount) ?? 0m;

        // Mission-reward coins are free promo credit and must not be cashed out. They are treated as
        // spent FIRST (which favours the user — it protects their real top-up money), so only the
        // portion of the balance backed by real money is withdrawable.
        var missionRewardTotal = await entries
            .Where(x => x.Type == TransactionType.MissionReward)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m;
        var purchaseSpend = -(await entries
            .Where(x => x.Type == TransactionType.Purchase)
            .SumAsync(x => (decimal?)x.Amount) ?? 0m); // Purchase amounts are stored negative

        return (balance, Services.WalletRules.Withdrawable(balance, missionRewardTotal, purchaseSpend));
    }

    public async Task<(List<Withdraw> Items, int TotalCount)> GetMineAsync(
        int userId,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Withdraws
            .AsNoTracking()
            .Include(x => x.Wallet)
            .Where(x => x.Wallet.UserId == userId);

        return await ToPagedResultAsync(query, pageNumber, pageSize);
    }

    public async Task<(List<Withdraw> Items, int TotalCount)> GetAdminAsync(
        WithdrawStatus? status,
        string? search,
        int pageNumber,
        int pageSize)
    {
        var query = _context.Withdraws
            .AsNoTracking()
            .Include(x => x.Wallet)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            var isUserId = int.TryParse(keyword, out var userId);
            query = query.Where(x =>
                x.AccountName.Contains(keyword) ||
                x.BankName.Contains(keyword) ||
                x.BankAccount.Contains(keyword) ||
                (isUserId && x.Wallet.UserId == userId));
        }

        return await ToPagedResultAsync(query, pageNumber, pageSize);
    }

    public async Task<(Withdraw? Withdraw, decimal Balance, bool NotFound, bool AlreadyProcessed)> UpdateStatusAsync(
        Guid id,
        WithdrawStatus status,
        string? note)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var withdraw = await _context.Withdraws
            .Include(x => x.Wallet)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (withdraw is null)
        {
            return (null, 0m, true, false);
        }

        if (withdraw.Status != WithdrawStatus.Pending)
        {
            return (withdraw, withdraw.Wallet.Balance, false, true);
        }

        withdraw.Status = status;
        withdraw.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        withdraw.ProcessedAt = DateTime.UtcNow;
        withdraw.UpdatedAt = DateTime.UtcNow;

        if (status == WithdrawStatus.Rejected)
        {
            var ledgerBalance = await _context.CurrencyEntries
                .Where(x => x.WalletId == withdraw.WalletId)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            // The withdrawal didn't happen, so give back what was deducted: the amount AND the fee.
            var refund = withdraw.Amount + Services.WalletRules.WithdrawFee(withdraw.Amount);

            withdraw.Wallet.Balance = ledgerBalance + refund;
            withdraw.Wallet.UpdatedAt = DateTime.UtcNow;
            // ReferenceId null: withdraw.Id is already used by the pending-withdrawal entry, and
            // ReferenceId is globally unique — reusing it here would throw.
            _context.CurrencyEntries.Add(new CurrencyEntry
            {
                WalletId = withdraw.WalletId,
                Amount = refund,
                Type = TransactionType.Refund,
                Description = $"Refund (amount + fee) for rejected withdrawal {withdraw.Id}",
                ReferenceId = null
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (withdraw, withdraw.Wallet.Balance, false, false);
    }

    private static async Task<(List<Withdraw> Items, int TotalCount)> ToPagedResultAsync(
        IQueryable<Withdraw> query,
        int pageNumber,
        int pageSize)
    {
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
