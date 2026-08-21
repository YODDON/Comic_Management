using System.Data;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Enums;
using WalletAPI.Data;
using WalletAPI.DTOs;
using WalletAPI.Entities;
using WalletAPI.Interfaces;

namespace WalletAPI.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly WalletDbContext _context;

    public WalletRepository(WalletDbContext context) => _context = context;

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<WalletCreditResultDto> CreditAsync(
        int userId, decimal amount, Guid referenceId, TransactionType type, string description)
    {
        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        
        try
        {
            if (await _context.CurrencyEntries.AnyAsync(entry => entry.ReferenceId == referenceId))
            {
                var existing = await _context.Wallets.AsNoTracking().SingleOrDefaultAsync(wallet => wallet.UserId == userId);
                return new WalletCreditResultDto(true, existing?.Balance ?? 0m);
            }

            var wallet = await _context.Wallets.SingleOrDefaultAsync(item => item.UserId == userId);
            if (wallet is null)
            {
                wallet = new Wallet { UserId = userId };
                _context.Wallets.Add(wallet);
            }

            wallet.Balance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            _context.CurrencyEntries.Add(new CurrencyEntry
            {
                Wallet = wallet,
                Amount = amount,
                Type = type,
                ReferenceId = referenceId,
                Description = description
            });
            await _context.SaveChangesAsync();
            if (!isExternalTransaction) await transaction!.CommitAsync();
            
            return new WalletCreditResultDto(false, wallet.Balance);
        }
        catch
        {
            if (!isExternalTransaction) await transaction!.RollbackAsync();
            throw;
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }

    public async Task<WalletDebitResultDto> DebitAsync(
        int userId, decimal amount, Guid referenceId, string description)
    {
        var isExternalTransaction = _context.Database.CurrentTransaction != null;
        var transaction = isExternalTransaction ? null : await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        
        try
        {
            if (await _context.CurrencyEntries.AnyAsync(entry => entry.ReferenceId == referenceId))
            {
                var processed = await _context.Wallets.AsNoTracking().SingleOrDefaultAsync(wallet => wallet.UserId == userId);
                return new WalletDebitResultDto(true, true, false, processed?.Balance ?? 0m);
            }

            var wallet = await _context.Wallets.SingleOrDefaultAsync(item => item.UserId == userId);
            if (wallet is null || wallet.Balance < amount)
                return new WalletDebitResultDto(false, false, true, wallet?.Balance ?? 0m);

            wallet.Balance -= amount;
            wallet.UpdatedAt = DateTime.UtcNow;
            _context.CurrencyEntries.Add(new CurrencyEntry
            {
                WalletId = wallet.Id,
                Amount = -amount,
                Type = TransactionType.Purchase,
                ReferenceId = referenceId,
                Description = description
            });
            await _context.SaveChangesAsync();
            if (!isExternalTransaction) await transaction!.CommitAsync();
            
            return new WalletDebitResultDto(true, false, false, wallet.Balance);
        }
        catch
        {
            if (!isExternalTransaction) await transaction!.RollbackAsync();
            throw;
        }
        finally
        {
            if (transaction != null) await transaction.DisposeAsync();
        }
    }
}
