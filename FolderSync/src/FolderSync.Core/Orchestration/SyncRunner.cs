using System.Diagnostics;
using FolderSync.Core.Diff;
using FolderSync.Core.Operations;
using FolderSync.Core.Scanning;
using FolderSync.Core.Scheduling;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Orchestration;

public class SyncRunner
{
    private readonly ILogger<SyncRunner> _logger;
    private readonly IDirectoryScanner _scanner;
    private readonly DiffEngine _diff;
    private readonly CopyEngine _copy;
    private readonly DeletionEngine _delete;
    
    public SyncRunner(ILogger<SyncRunner> logger, IDirectoryScanner scanner, DiffEngine diff, CopyEngine copy, DeletionEngine delete)
    {
        _logger = logger;
        _scanner = scanner;
        _diff = diff;
        _copy = copy;
        _delete = delete;
    }
    
    public async Task<SyncSummary> RunOnceAsync(string source, string replica, CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        var sw = new Stopwatch();
        sw.Start();
        var sourceSnap = await _scanner.BuildSnapshotAsync(source, ct);
        var replicaSnap = await _scanner.BuildSnapshotAsync(replica, ct);
        var diff = _diff.Compute(sourceSnap, replicaSnap);
        var copyStats = await _copy.ApplyAsync(sourceSnap, replicaSnap, diff, ct);
        var delStats = await _delete.DeleteAsync(sourceSnap, replicaSnap, diff, ct);
        sw.Stop();

        var summary = new SyncSummary
        {
            FilesCopied = copyStats.FilesCopied,
            FilesUpdated = copyStats.FilesUpdated,
            FilesDeleted = delStats.FilesDeleted,
            DirsCreated = copyStats.DirsCreated,
            DirsDeleted = delStats.DirsDeleted,
            Elapsed = sw.Elapsed,
            StartedUtc = start,
            FinishedUtc = DateTime.UtcNow,
        };
        
        _logger.LogInformation("Summary: {Summary}", summary);
        return summary;
    }
}