using System.IO.Abstractions;
using FolderSync.Core.Results;
using FolderSync.Core.Sync.Scanning;
using FolderSync.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Sync.Operations;

public class DeletionEngine(ILogger<DeletionEngine> logger, IFileSystem fs)
{
    public Task<OperationStats> Run(DirectorySnapshot replica, DiffResult diff,
        OperationStats stats,
        CancellationToken ct = default)
    {
        var ctx = new DeletionContext(fs, logger, replica.RootPath, stats);
        ctx.RemoveDirectories(diff.DirsToDelete, ct);
        ctx.RemoveFiles(diff.FilesToDelete, ct);

        return Task.FromResult(stats);
    }

    private sealed class DeletionContext(IFileSystem fs, ILogger logger, string replicaRoot, OperationStats stats)
    {
        internal void RemoveFiles(IEnumerable<string> filesToRemove, CancellationToken ct)
        {
            filesToRemove.Where(path => fs.File.Exists(fs.Path.Combine(replicaRoot, path)))
                .ToList()
                .ForEach(path =>
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();
                        if (fs.File.GetAttributes(fs.Path.Combine(replicaRoot, path)).HasFlag(FileAttributes.ReadOnly))
                            fs.File.SetAttributes(fs.Path.Combine(replicaRoot, path),
                                fs.File.GetAttributes(fs.Path.Combine(replicaRoot, path)) & ~FileAttributes.ReadOnly);

                        fs.File.Delete(fs.Path.Combine(replicaRoot, path));
                        stats.FilesDeleted++;
                        logger.LogDebug("Removed file: {Path}", path);
                    }
                    catch (Exception ex) when (ex.IsBenign())
                    {
                        logger.LogWarning("Failed to delete file {File}: {Error}", path, ex.Message);
                        logger.LogDebug(ex, "Stacktrace: ");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Unexpected error");
                    }
                });
        }

        internal void RemoveDirectories(IEnumerable<string> directoriesToRemove, CancellationToken ct)
        {
            directoriesToRemove
                .Where(path =>
                    fs.Directory.Exists(fs.Path.Combine(replicaRoot, path)) &&
                    !fs.Directory.EnumerateFileSystemEntries(fs.Path.Combine(replicaRoot, path)).Any())
                .OrderByDescending(d => d.Count(ch => ch == fs.Path.DirectorySeparatorChar))
                .ToList()
                .ForEach(path =>
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();
                        path = fs.Path.Combine(replicaRoot, path);
                        fs.Directory.Delete(path);
                        stats.DirsDeleted++;
                        logger.LogDebug("Removed dir: {Path}", path);
                    }
                    catch (Exception ex) when (ex.IsBenign())
                    {
                        logger.LogWarning("Failed to delete directory {Dir}: {Error}", path, ex.Message);
                        logger.LogDebug(ex, "Stacktrace: ");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Unexpected error");
                    }
                });
        }
    }
}