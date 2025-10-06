using FolderSync.Core.Configuration;
using FolderSync.Core.Diff;
using FolderSync.Core.Operations;
using FolderSync.Core.Orchestration;
using FolderSync.Core.Scanning;
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
        var syncLoop = new SyncLoop(syncRunner, opts.SourcePath, opts.ReplicaPath, opts.Interval);

        var cts = new CancellationTokenSource();
        return (syncLoop, cts);
    }
}