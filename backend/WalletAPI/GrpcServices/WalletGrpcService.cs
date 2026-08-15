using Grpc.Core;
using WalletAPI.Interfaces;
using WalletAPI.Protos;

namespace WalletAPI.GrpcServices;

public class WalletGrpcService : WalletService.WalletServiceBase
{
    private readonly IWalletApplicationService _walletService;

    public WalletGrpcService(IWalletApplicationService walletService) => _walletService = walletService;

    public override async Task<AddCoinResponse> AddCoin(AddCoinRequest request, ServerCallContext context)
    {
        if (!TryValidate(request.UserId, request.Amount, request.ReferenceId,
                out var userId, out var amount, out var referenceId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid wallet request."));

        var result = await _walletService.AddCoinAsync(
            userId, amount, referenceId, request.CreditType, request.Description);
        return new AddCoinResponse
        {
            Success = true,
            Message = result.AlreadyProcessed ? "Already processed." : "Coins added successfully."
        };
    }

    public override async Task<DebitCoinResponse> DebitCoin(DebitCoinRequest request, ServerCallContext context)
    {
        if (!TryValidate(request.UserId, request.Amount, request.ReferenceId,
                out var userId, out var amount, out var referenceId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid wallet request."));

        var result = await _walletService.DebitCoinAsync(
            userId, amount, referenceId, request.Description);
        return new DebitCoinResponse
        {
            Success = result.Success,
            AlreadyProcessed = result.AlreadyProcessed,
            InsufficientBalance = result.InsufficientBalance,
            Balance = (double)result.Balance,
            Message = result.AlreadyProcessed
                ? "Already processed."
                : result.InsufficientBalance
                    ? "Insufficient balance."
                    : "Coins debited successfully."
        };
    }

    private static bool TryValidate(
        string rawUserId,
        double rawAmount,
        string rawReferenceId,
        out int userId,
        out decimal amount,
        out Guid referenceId)
    {
        amount = (decimal)rawAmount;
        referenceId = Guid.Empty;
        return int.TryParse(rawUserId, out userId) && userId > 0 && amount > 0
            && Guid.TryParse(rawReferenceId, out referenceId);
    }
}
