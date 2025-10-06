using FolderSync.Core.Copying;
using FolderSync.Core.Deletion;
using FolderSync.Core.Diff;
using FolderSync.Core.Options;
using FolderSync.Core.Scanning;
using FolderSync.Core.Scheduling;
using Microsoft.Extensions.Logging;

namespace FolderSync.App;

public static class Bootstrapper
{
    public static (SyncLoop, CancellationTokenSource) Build(ILoggerFactory lf, SyncOptions opts)
    {
        var scanner = new DirectoryScanner(lf.CreateLogger<DirectoryScanner>());
        var engine = new DiffEngine(lf.CreateLogger<DiffEngine>());
        var copyEngine = new CopyEngine(lf.CreateLogger<CopyEngine>());
        var deletionEngine = new DeletionEngine(lf.CreateLogger<DeletionEngine>());
        var syncRunner = new SyncRunner(lf.CreateLogger<SyncRunner>(), scanner, engine, copyEngine, deletionEngine);
        var syncLoop = new SyncLoop(lf.CreateLogger<SyncLoop>(), syncRunner, opts.SourcePath, opts.ReplicaPath, opts.Interval);

        var cts = new CancellationTokenSource();
        return (syncLoop, cts);
    }
}