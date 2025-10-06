using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Scheduling;

public class SyncLoop
{
    private readonly ILogger<SyncLoop> _logger;
    private readonly SyncRunner _syncRunner;
    private readonly string _source;
    private readonly string _replica;
    private readonly TimeSpan _interval;
    
    public SyncLoop(ILogger<SyncLoop> logger, SyncRunner syncRunner, string source, string replica, TimeSpan interval)
    {
        _logger = logger;
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