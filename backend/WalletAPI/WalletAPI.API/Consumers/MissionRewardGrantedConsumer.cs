using MassTransit;
using SharedKernel.Events;
using WalletAPI.Interfaces;

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
