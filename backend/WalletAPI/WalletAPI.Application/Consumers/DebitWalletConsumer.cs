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
    public class DebitWalletConsumer : IConsumer<DebitWalletCommand>
    {
        private readonly IWalletRepository _repository;

        public DebitWalletConsumer(IWalletRepository repository)
        {
            _repository = repository;
        }

        public async Task Consume(ConsumeContext<DebitWalletCommand> context)
        {
            var msg = context.Message;
            
            try
            {
                var result = await _repository.DebitAsync(
                    msg.UserId,
                    msg.Amount,
                    msg.TransactionId,
                    $"Mở khóa chapter qua Saga, Transaction: {msg.TransactionId}");

                if (result.Success || result.AlreadyProcessed)
                {
                    await context.Publish(new WalletDebitedEvent(msg.CorrelationId, result.Balance));
                }
                else
                {
                    var reason = result.InsufficientBalance ? "Số dư Dâu không đủ để thực hiện giao dịch." : "Giao dịch không thành công.";
                    await context.Publish(new WalletDebitFailedEvent(msg.CorrelationId, reason));
                }
            }
            catch (Exception ex)
            {
                await context.Publish(new WalletDebitFailedEvent(msg.CorrelationId, $"Lỗi hệ thống khi trừ Dâu: {ex.Message}"));
            }
        }
    }
}
