using System;
using System.Threading.Tasks;

namespace MissionAPI.Interfaces
{
    public interface IWalletGrpcClient
    {
        Task<bool> AddCoinAsync(int userId, decimal amount, Guid referenceId, string description);
    }
}
