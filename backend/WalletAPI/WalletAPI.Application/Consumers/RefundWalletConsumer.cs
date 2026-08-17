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
                    referenceId,
                    TransactionType.Refund,
                    $"Hoàn tiền giao dịch mở khóa lỗi qua Saga, Transaction: {msg.TransactionId}");

                // Even if not Success (e.g. idempotency failure means already credited), we publish Refunded.
                // CreditAsync handles idempotency gracefully.
                await context.Publish(new WalletRefundedEvent(msg.CorrelationId));
            }
            catch (Exception)
            {
                // If it fails, MassTransit will retry based on its configuration, which is standard for Compensating Actions.
                throw;
            }
        }
    }
}
