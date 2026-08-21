using System;
using System.Threading.Tasks;
using MassTransit;
using PaymentAPI.Entities;
using PaymentAPI.Interfaces;
using SharedKernel.Contracts.Purchase;
using SharedKernel.Enums;

namespace PaymentAPI.Application.Consumers
{
    public class PurchaseCompletedConsumer : IConsumer<PurchaseCompletedEvent>
    {
        private readonly IPaymentRepository _repository;

        public PurchaseCompletedConsumer(IPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<PurchaseCompletedEvent> context)
        {
            var msg = context.Message;
            
            await _repository.BeginTransactionAsync();
            var transaction = await _repository.GetTransactionByIdAsync(msg.TransactionId);
            if (transaction == null || transaction.Status == TransactionStatus.Completed)
            {
                await _repository.CommitTransactionAsync();
                return;
            }

            transaction.Status = TransactionStatus.Completed;
            await _repository.UpdateTransactionAsync(transaction);

            // Create UserPurchase
            var existingPurchase = await _repository.GetUserPurchaseAsync(transaction.UserId, transaction.ChapterId.Value);
            if (existingPurchase == null)
            {
                await _repository.AddUserPurchaseAsync(new UserPurchase
                {
                    UserId = transaction.UserId,
                    ComicId = msg.ComicId, // We need ComicId! Wait, SubmitPurchaseCommand needs ComicId!
                    ChapterId = transaction.ChapterId.Value,
                    Price = transaction.Amount,
                    PurchasedAt = DateTime.UtcNow
                });
            }

            await _repository.CommitTransactionAsync();
        }
    }
}
