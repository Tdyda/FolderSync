using System.IO.Abstractions;
using FolderSync.Core.Results;
using FolderSync.Core.Sync.Scanning;
using FolderSync.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Sync.Operations;

public class DeletionEngine(ILogger<DeletionEngine> logger, IFileSystem fs)
{
    public Task<DelStats> ExecAsync(DirectorySnapshot source, DirectorySnapshot replica, DiffResult diff,
        CancellationToken ct = default)
    {
        var del = new DelStats();

        DeleteFiles(replica.RootPath, diff.FilesToDelete, del, ct);
        DeleteDirectories(replica.RootPath, diff.DirsToDelete, del, ct);

        return Task.FromResult(del);
    }

    private void DeleteFiles(string root, IEnumerable<string> filesToDelete, DelStats del, CancellationToken ct)
    {
        foreach (var relFile in filesToDelete)
        {
            ct.ThrowIfCancellationRequested();
            var target = fs.Path.Combine(root, relFile);
            try
            {
                if (fs.File.Exists(target))
                {
                    fs.File.SetAttributes(target, FileAttributes.Normal);
                    fs.File.Delete(target);
                    del.FilesDeleted++;
                    logger.LogInformation("Deleted file {File}", target);
                }
            }
            catch (Exception ex) when (ex.IsBenign())
            {
                if (!logger.IsEnabled(LogLevel.Debug))
                    logger.LogWarning("Failed to delete file {File}: {Error}", target, ex.Message);
                logger.LogDebug(ex, "Failed to delete file {File}", target);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error");
            }
        }
    }

    private void DeleteDirectories(string root, IEnumerable<string> dirsToDelete, DelStats del, CancellationToken ct)
    {
        foreach (var relDir in OrderDirsByDepthDesc(dirsToDelete))
        {
            ct.ThrowIfCancellationRequested();
            var target = fs.Path.Combine(root, relDir);

            try
            {
                var isEmpty = !fs.Directory.EnumerateDirectories(target).Any();
                if (fs.Directory.Exists(target) && isEmpty)
                {
                    TryUnsetReadOnly(target);
                    fs.Directory.Delete(target, false);
                    del.DirsDeleted++;
                    logger.LogInformation("Deleted directory {Dir}", target);
                }
            }
            catch (Exception ex) when (ex.IsBenign())
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogWarning("Failed to delete directory {Dir}: {Error}", target, ex.Message);
                logger.LogDebug(ex, "Failed to delete directory {Dir}", target);
            }
            catch (Exception ex)
            {
                logger.LogError("Unexpected error");
                logger.LogDebug(ex, "Unexpected error");
            }
        }
    }

    private void TryUnsetReadOnly(string path)
    {
        var attr = fs.File.GetAttributes(path);
        try
        {
            if (attr.HasFlag(FileAttributes.ReadOnly)) fs.File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogWarning("Failed to unset read only for file {File}", path);
            logger.LogDebug(ex, "Failed to unset read only for file {File}", path);
        }
    }

    private IEnumerable<string> OrderDirsByDepthDesc(IEnumerable<string> dirsToDelete)
    {
        return dirsToDelete.OrderByDescending(d => d.Count(ch => ch == fs.Path.DirectorySeparatorChar));
    }
}