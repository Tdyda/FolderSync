using System.IO.Abstractions;
using FolderSync.Core.Interfaces;
using FolderSync.Core.Results;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Sync.Operations;

public sealed class CopyEngine(IFileSystem fs, IFileCopier fileCopier, ILogger<CopyEngine> log)
{
    public async Task<OperationStats> Run(string sourceRoot, string replicaRoot, IEnumerable<string> filesToCopy,
        IEnumerable<string> filesToUpdate, IEnumerable<string> directoriesToCreate, OperationStats stats,
        CancellationToken ct)
    {
        var ctx = new CopyContext(fs, fileCopier, log, sourceRoot, replicaRoot);

        ctx.CreateDir(replicaRoot, directoriesToCreate, stats);
        await ctx.SyncFiles(filesToCopy, s => s.FilesCopied++, stats, ct);
        await ctx.SyncFiles(filesToUpdate, s => s.FilesUpdated++, stats, ct);

        return stats;
    }

    private sealed class CopyContext(
        IFileSystem fs,
        IFileCopier fileCopier,
        ILogger logger,
        string sourceRoot,
        string replicaRoot)
    {
        internal void CreateDir(string rootPath, IEnumerable<string> directoriesToCreate,
            OperationStats stats)
        {
            directoriesToCreate.Where(path => !fs.Directory.Exists(fs.Path.Combine(rootPath, path)))
                .ToList()
                .ForEach(path =>
                {
                    path = fs.Path.Combine(rootPath, path);
                    fs.Directory.CreateDirectory(path);
                    stats.DirsCreated++;
                    logger.LogDebug("Create dir {Path}", path);
                });
        }

        public async Task SyncFiles(IEnumerable<string> relPaths, Action<OperationStats> bump, OperationStats stats,
            CancellationToken ct)
        {
            foreach (var rel in relPaths)
            {
                var src = fs.Path.Combine(sourceRoot, rel);
                var dst = fs.Path.Combine(replicaRoot, rel);
                fs.Directory.CreateDirectory(fs.Path.GetDirectoryName(dst)!);

                fs.File.SetLastWriteTimeUtc(dst, fs.File.GetLastWriteTimeUtc(src));
                await fileCopier.AtomicCopyAsync(src, dst, ct);
                var beforeOperation = stats.FilesCopied;
                bump(stats);
                var verb = stats.FilesCopied != beforeOperation ? "Copied" : "Updated";
                logger.LogDebug("{Verb}: {Rel}", verb, rel);
            }
        }
    }
}