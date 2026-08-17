using System;
using System.Threading.Tasks;
using MassTransit;
using SharedKernel.Contracts.Purchase;
using SharedKernel.Enums;
using WalletAPI.Interfaces;
using MediatR;
using WalletAPI.Application.Features.Currency.Queries;
using WalletAPI.Application.Features.Wallet.Commands;
using WalletAPI.Application.Features.Withdraw.Commands;
using WalletAPI.Application.Features.Withdraw.Queries;

namespace WalletAPI.Application.Consumers
{
    public class RefundWalletConsumer : IConsumer<RefundWalletCommand>
    {
        private readonly IWalletRepository _repository;

        public RefundWalletConsumer(IWalletRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<RefundWalletCommand> context)
        {
            var msg = context.Message;
            
            var bytes = msg.TransactionId.ToByteArray();
            bytes[15] ^= 0xFF;
            var referenceId = new Guid(bytes);
            try
            {
                var result = await _repository.CreditAsync(
                    msg.UserId,
                    msg.Amount,
                    msg.TransactionId,
                    TransactionType.Refund,
                    $"Hoàn tiền do lỗi mở chapter, Transaction: {msg.TransactionId}");

                await context.Publish(new WalletRefundedEvent(msg.CorrelationId));
                await _repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // In a real production system, log this failure or move to DLQ.
                // It means the saga cannot complete the refund gracefully.
                Console.WriteLine($"[CRITICAL] Hoàn tiền thất bại cho giao dịch {msg.TransactionId}: {ex.Message}");
                throw;
            }
        }
    }
}
