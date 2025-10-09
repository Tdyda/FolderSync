using System.IO.Abstractions;
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
        IFileSystem fs =  new FileSystem();
        IFileOps filesOps = new FileOps(fs);
        IDirectoryScanner scanner = new DirectoryScanner(lf.CreateLogger<DirectoryScanner>(), fs);
        var engine = new DiffEngine(lf.CreateLogger<DiffEngine>());
        var copyEngine = new CopyEngine(lf.CreateLogger<CopyEngine>(), fs, filesOps);
        var deletionEngine = new DeletionEngine(lf.CreateLogger<DeletionEngine>(), fs);
        var syncRunner = new SyncRunner(lf.CreateLogger<SyncRunner>(), scanner, engine, copyEngine, deletionEngine);
        var syncLoop = new SyncLoop(syncRunner, opts.SourcePath, opts.ReplicaPath, opts.Interval);

        var cts = new CancellationTokenSource();
        return (syncLoop, cts);
    }
}