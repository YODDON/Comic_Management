using MassTransit;
using SharedKernel.Events;
using WalletAPI.Interfaces;
using MediatR;
using WalletAPI.Application.Features.Currency.Queries;
using WalletAPI.Application.Features.Wallet.Commands;
using WalletAPI.Application.Features.Withdraw.Commands;
using WalletAPI.Application.Features.Withdraw.Queries;

namespace WalletAPI.Consumers;

public class MissionRewardGrantedConsumer : IConsumer<MissionRewardGrantedEvent>
{
    private readonly IMediator _mediator;

    public MissionRewardGrantedConsumer(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<MissionRewardGrantedEvent> context)
    {
        var msg = context.Message;
        await _mediator.Send(new AddCoinCommand
        {
            UserId = msg.UserId, 
            Amount = msg.CoinAmount, 
            ReferenceId = msg.ReferenceId, 
            CreditType = "MissionReward", 
            Description = msg.Description
        });
    }
}
