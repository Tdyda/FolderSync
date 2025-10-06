using System.Diagnostics;
using FolderSync.Core.Copying;
using FolderSync.Core.Deletion;
using FolderSync.Core.Diff;
using FolderSync.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Scheduling;

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
        
        _logger.LogInformation("Scanning source and replica...");
        var sourceSnap = await _scanner.BuildSnapshotAsync(source, ct);
        var replicaSnap = await _scanner.BuildSnapshotAsync(replica, ct);
        
        _logger.LogInformation("Computing diff...");
        var diff = _diff.Compute(sourceSnap, replicaSnap);
        
        _logger.LogInformation("Applying copy/update operations...");
        await _copy.ApplyAsync(sourceSnap, replicaSnap, diff, ct);
        
        _logger.LogInformation("Apply deletions...");
        await _delete.DeleteAsync(sourceSnap, replicaSnap, diff, ct);
        
        sw.Stop();

        var summary = new SyncSummary
        {
            FilesCopied = diff.FilesToCopy.Count,
            FilesUpdated = diff.FilesToUpdate.Count,
            FilesDeleted = diff.FilesToDelete.Count,
            DirsCreated = diff.DirsToCreate.Count,
            DirsDeleted = diff.DirsToDelete.Count,
            Elapsed = sw.Elapsed,
            StartedUtc = start,
            FinishedUtc = DateTime.UtcNow,
        };
        
        _logger.LogInformation("Summary: {Summary}", summary);
        return summary;
    }
}