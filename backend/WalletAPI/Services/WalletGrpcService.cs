using System.Data;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Enums;
using WalletAPI.Data;
using WalletAPI.Entities;
using WalletAPI.Protos;

namespace WalletAPI.Services;

public class WalletGrpcService : WalletService.WalletServiceBase
{
    private readonly WalletDbContext _context;

    public WalletGrpcService(WalletDbContext context)
    {
        _context = context;
    }

    public override async Task<AddCoinResponse> AddCoin(AddCoinRequest request, ServerCallContext context)
    {
        if (!TryValidate(request.UserId, request.Amount, request.ReferenceId, out var userId, out var amount, out var referenceId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid wallet request."));

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await _context.CurrencyEntries.AnyAsync(x => x.ReferenceId == referenceId))
            return new AddCoinResponse { Success = true, Message = "Already processed." };

        var wallet = await _context.Wallets.SingleOrDefaultAsync(x => x.UserId == userId);
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
            Type = WalletRules.ResolveCreditType(request.CreditType),
            ReferenceId = referenceId,
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Coin credit" : request.Description
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return new AddCoinResponse { Success = true, Message = "Coins added successfully." };
    }

    public override async Task<DebitCoinResponse> DebitCoin(DebitCoinRequest request, ServerCallContext context)
    {
        if (!TryValidate(request.UserId, request.Amount, request.ReferenceId, out var userId, out var amount, out var referenceId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid wallet request."));

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (await _context.CurrencyEntries.AnyAsync(x => x.ReferenceId == referenceId))
        {
            var processedWallet = await _context.Wallets.SingleOrDefaultAsync(x => x.UserId == userId);
            return new DebitCoinResponse
            {
                Success = true,
                AlreadyProcessed = true,
                Balance = (double)(processedWallet?.Balance ?? 0m),
                Message = "Already processed."
            };
        }

        var wallet = await _context.Wallets.SingleOrDefaultAsync(x => x.UserId == userId);
        if (wallet is null || wallet.Balance < amount)
            return new DebitCoinResponse
            {
                Success = false,
                InsufficientBalance = true,
                Balance = (double)(wallet?.Balance ?? 0m),
                Message = "Insufficient balance."
            };

        wallet.Balance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        _context.CurrencyEntries.Add(new CurrencyEntry
        {
            WalletId = wallet.Id,
            Amount = -amount,
            Type = TransactionType.Purchase,
            ReferenceId = referenceId,
            Description = string.IsNullOrWhiteSpace(request.Description) ? "Chapter purchase" : request.Description
        });
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return new DebitCoinResponse
        {
            Success = true,
            Balance = (double)wallet.Balance,
            Message = "Coins debited successfully."
        };
    }

    private static bool TryValidate(string rawUserId, double rawAmount, string rawReferenceId,
        out int userId, out decimal amount, out Guid referenceId)
    {
        amount = (decimal)rawAmount;
        referenceId = Guid.Empty;
        return int.TryParse(rawUserId, out userId) && userId > 0 && amount > 0
            && Guid.TryParse(rawReferenceId, out referenceId);
    }

}
