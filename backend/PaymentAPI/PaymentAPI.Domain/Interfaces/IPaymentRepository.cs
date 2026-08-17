using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PaymentAPI.Entities;

namespace PaymentAPI.Interfaces
{
    public interface IPaymentRepository
    {
        Task<List<Transaction>> GetUserTransactionsAsync(int userId);
        Task<(List<Transaction> Items, int TotalCount)> GetTransactionsAsync(
            int? userId,
            string? type,
            string? status,
            string? search,
            int pageNumber,
            int pageSize);
        Task<Transaction?> GetTransactionByIdAsync(Guid id);
        Task<Transaction?> GetTransactionByCodeAsync(string transactionCode);
        Task<Transaction?> GetTransactionByNotePrefixAsync(string notePrefix);
        Task<UserPurchase?> GetUserPurchaseAsync(int userId, Guid chapterId);
        Task AddTransactionAsync(Transaction transaction);
        Task UpdateTransactionAsync(Transaction transaction);
        Task AddUserPurchaseAsync(UserPurchase userPurchase);
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task SaveFailedTransactionAsync(Transaction transaction);
    }
}
