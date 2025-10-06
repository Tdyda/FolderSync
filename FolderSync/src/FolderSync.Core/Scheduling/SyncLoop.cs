namespace FolderSync.Core.Scheduling;

public class SyncLoop
{
    private readonly SyncRunner _syncRunner;
    private readonly string _source;
    private readonly string _replica;
    private readonly TimeSpan _interval;
    
    public SyncLoop(SyncRunner syncRunner, string source, string replica, TimeSpan interval)
    {
        _syncRunner = syncRunner;
        _source = source;
        _replica = replica;
        _interval = interval;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        await _syncRunner.RunOnceAsync(_source, _replica, ct);
        
        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await _syncRunner.RunOnceAsync(_source, _replica, ct);
        }
    }
}