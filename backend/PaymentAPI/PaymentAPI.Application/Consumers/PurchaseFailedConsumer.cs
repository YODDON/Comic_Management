using System.Threading.Tasks;
using MassTransit;
using PaymentAPI.Interfaces;
using SharedKernel.Contracts.Purchase;
using SharedKernel.Enums;

namespace PaymentAPI.Application.Consumers
{
    public class PurchaseFailedConsumer : IConsumer<PurchaseFailedEvent>
    {
        private readonly IPaymentRepository _repository;

        public PurchaseFailedConsumer(IPaymentRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<PurchaseFailedEvent> context)
        {
            var msg = context.Message;
            
            await _repository.BeginTransactionAsync();
            var transaction = await _repository.GetTransactionByIdAsync(msg.TransactionId);
            if (transaction == null || transaction.Status != TransactionStatus.Pending)
            {
                await _repository.CommitTransactionAsync();
                return;
            }

            transaction.Status = TransactionStatus.Failed;
            transaction.Note = msg.Reason;
            await _repository.UpdateTransactionAsync(transaction);
            await _repository.CommitTransactionAsync();
        }
    }
}
