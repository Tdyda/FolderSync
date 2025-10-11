using System.Diagnostics;
using FolderSync.Core.Results;
using FolderSync.Core.Sync.Diff;
using FolderSync.Core.Sync.Operations;
using FolderSync.Core.Sync.Scanning;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Application;

public class SyncRunner(
    ILogger<SyncRunner> logger,
    DirectoryScanner scanner,
    DiffEngine diff,
    CopyEngine copy,
    DeletionEngine delete)
{
    public async Task<SyncSummary> RunOnceAsync(string source, string replica, CancellationToken ct)
    {
        var start = DateTime.UtcNow;
        var sw = new Stopwatch();
        sw.Start();
        var sourceSnap = await scanner.BuildSnapshotAsync(source, ct);
        var replicaSnap = await scanner.BuildSnapshotAsync(replica, ct);
        var diff1 = diff.Compute(sourceSnap, replicaSnap);
        var copyStats = await copy.ExecAsync(sourceSnap, replicaSnap, diff1, ct);
        var delStats = await delete.ExecAsync(sourceSnap, replicaSnap, diff1, ct);
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
            FinishedUtc = DateTime.UtcNow
        };

        logger.LogInformation("Summary: {Summary}", summary);
        return summary;
    }
}