using Microsoft.EntityFrameworkCore;
using System.Data;
using SharedKernel.Enums;
using WalletAPI.Data;
using WalletAPI.Entities;
using WalletAPI.Interfaces;

namespace WalletAPI.Repositories;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly WalletDbContext _context;

    public CurrencyRepository(WalletDbContext context)
    {
        _context = context;
    }

    public async Task<(List<CurrencyEntry> Items, int TotalCount, decimal Balance)> GetHistoryAsync(
        int userId,
        int pageNumber,
        int pageSize)
    {
        var wallet = await _context.Wallets
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId);

        if (wallet is null)
        {
            return (new List<CurrencyEntry>(), 0, 0m);
        }

        var query = _context.CurrencyEntries
            .AsNoTracking()
            .Where(x => x.WalletId == wallet.Id);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount, wallet.Balance);
    }

    public async Task<(CurrencyEntry? Entry, decimal Balance, bool InsufficientBalance)> CreateEntryAsync(
        int userId,
        decimal amount,
        TransactionType type,
        string description)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var wallet = await _context.Wallets.SingleOrDefaultAsync(x => x.UserId == userId);
        if (wallet is null)
        {
            if (amount < 0)
            {
                return (null, 0m, true);
            }

            wallet = new Wallet
            {
                UserId = userId,
                Balance = 0m
            };
            _context.Wallets.Add(wallet);
        }

        var newBalance = wallet.Balance + amount;
        if (newBalance < 0)
        {
            return (null, wallet.Balance, true);
        }

        wallet.Balance = newBalance;
        wallet.UpdatedAt = DateTime.UtcNow;

        var entry = new CurrencyEntry
        {
            Wallet = wallet,
            WalletId = wallet.Id,
            Amount = amount,
            Type = type,
            Description = description.Trim()
        };

        _context.CurrencyEntries.Add(entry);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return (entry, wallet.Balance, false);
    }
}
