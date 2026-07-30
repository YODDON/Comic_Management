using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using MissionAPI.Interfaces;
using WalletAPI.Protos;

namespace MissionAPI.Services
{
    public class WalletGrpcClient : IWalletGrpcClient
    {
        private readonly string _walletApiUrl;

        public WalletGrpcClient(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _walletApiUrl = Environment.GetEnvironmentVariable("WALLET_API_URL")
                ?? configuration["GrpcEndpoints:WalletAPI"] ?? "http://localhost:5091";
        }

        public async Task<bool> AddCoinAsync(int userId, decimal amount, Guid referenceId, string description)
        {
            try
            {
                using var channel = GrpcChannel.ForAddress(_walletApiUrl);
                var client = new WalletService.WalletServiceClient(channel);
                var request = new AddCoinRequest
                {
                    UserId = userId.ToString(),
                    Amount = (double)amount,
                    ReferenceId = referenceId.ToString(),
                    Description = description
                    // Mission rewards are free coins and must NOT be withdrawable as cash. WalletAPI
                    // already treats an unset CreditType as MissionReward (fail-closed), so this is
                    // correct as-is. TODO(Nhựt): set CreditType = "MissionReward" explicitly for clarity.
                };

                var response = await client.AddCoinAsync(request);
                return response.Success;
            }
            catch (Exception ex)
            {
                // In production, log this exception
                Console.WriteLine($"Error calling WalletAPI AddCoin: {ex.Message}");
                return false;
            }
        }
    }
}
