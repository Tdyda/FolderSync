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
        var diffResult = diff.Compute(sourceSnap, replicaSnap);
        var operationStats = await copy.Run(sourceSnap.RootPath, replicaSnap.RootPath, diffResult.FilesToCopy,
            diffResult.FilesToUpdate, diffResult.DirsToCreate, new OperationStats(), ct);
        var delStats = await delete.Run(replicaSnap, diffResult, operationStats, ct);
        sw.Stop();

        var summary = new SyncSummary
        {
            FilesCopied = operationStats.FilesCopied,
            FilesUpdated = operationStats.FilesUpdated,
            FilesDeleted = delStats.FilesDeleted,
            DirsCreated = operationStats.DirsCreated,
            DirsDeleted = delStats.DirsDeleted,
            Elapsed = sw.Elapsed,
            StartedUtc = start,
            FinishedUtc = DateTime.UtcNow
        };

        logger.LogInformation("Summary: {Summary}{NewLine}", summary, Environment.NewLine);
        return summary;
    }
}