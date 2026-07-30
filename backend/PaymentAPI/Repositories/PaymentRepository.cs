using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PaymentAPI.Data;
using PaymentAPI.Entities;
using PaymentAPI.Interfaces;

namespace PaymentAPI.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _context;
        private IDbContextTransaction? _transaction;

        public PaymentRepository(PaymentDbContext context)
        {
            _context = context;
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(int userId)
        {
            return await _context.Transactions
                .Where(t => t.UserId == userId && t.Status == SharedKernel.Enums.TransactionStatus.Completed)
                .ToListAsync();
        }

        public async Task<(List<Transaction> Items, int TotalCount)> GetTransactionsAsync(
            int? userId,
            string? type,
            string? status,
            string? search,
            int pageNumber,
            int pageSize)
        {
            var query = _context.Transactions.AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(t => t.UserId == userId.Value);
            }

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<SharedKernel.Enums.TransactionType>(type, true, out var transactionType))
            {
                query = query.Where(t => t.Type == transactionType);
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<SharedKernel.Enums.TransactionStatus>(status, true, out var transactionStatus))
            {
                query = query.Where(t => t.Status == transactionStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                var isUserId = int.TryParse(keyword, out var searchedUserId);
                query = query.Where(t =>
                    t.TransactionCode.Contains(keyword) ||
                    (isUserId && t.UserId == searchedUserId));
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Transaction?> GetTransactionByIdAsync(Guid id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        public async Task<Transaction?> GetTransactionByCodeAsync(string transactionCode)
        {
            return await _context.Transactions.FirstOrDefaultAsync(t => t.TransactionCode == transactionCode);
        }

        public async Task<UserPurchase?> GetUserPurchaseAsync(int userId, Guid chapterId)
        {
            return await _context.UserPurchases
                .FirstOrDefaultAsync(u => u.UserId == userId && u.ChapterId == chapterId);
        }

        public async Task AddTransactionAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTransactionAsync(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task AddUserPurchaseAsync(UserPurchase userPurchase)
        {
            _context.UserPurchases.Add(userPurchase);
            await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task SaveFailedTransactionAsync(Transaction transaction)
        {
            _context.Entry(transaction).State = EntityState.Detached;
            transaction.Status = SharedKernel.Enums.TransactionStatus.Failed;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
