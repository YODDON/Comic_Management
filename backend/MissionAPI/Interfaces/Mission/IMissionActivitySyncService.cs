namespace MissionAPI.Interfaces;

public interface IMissionActivitySyncService
{
    Task SyncAsync(int userId, CancellationToken cancellationToken = default);
}
