namespace FolderSync.Core.Orchestration;

public class SyncLoop(SyncRunner syncRunner, string source, string replica, TimeSpan interval)
{
    public async Task RunAsync(CancellationToken ct)
    {
        await syncRunner.RunOnceAsync(source, replica, ct);
        
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await syncRunner.RunOnceAsync(source, replica, ct);
        }
    }
}