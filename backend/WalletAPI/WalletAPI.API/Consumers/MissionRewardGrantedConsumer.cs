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
    private readonly IWalletApplicationService _walletService;

    public MissionRewardGrantedConsumer(IWalletApplicationService walletService)
    {
        _walletService = walletService;
    }

    public async Task Consume(ConsumeContext<MissionRewardGrantedEvent> context)
    {
        var msg = context.Message;
        await _walletService.AddCoinAsync(
            msg.UserId, 
            msg.CoinAmount, 
            msg.ReferenceId, 
            "MissionReward", 
            msg.Description);
    }
}
