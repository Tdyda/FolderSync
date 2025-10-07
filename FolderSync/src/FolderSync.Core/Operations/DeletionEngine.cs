using FolderSync.Core.Common;
using FolderSync.Core.Extensions;
using FolderSync.Core.Results;
using FolderSync.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Operations;

public class DeletionEngine(ILogger<DeletionEngine> logger)
{
    public Task<DelStats> DeleteAsync(DirectorySnapshot source, DirectorySnapshot replica, DiffResult diff, CancellationToken ct = default)
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
            var target = Path.Combine(root, relFile);
            DeleteSingleFile(target, del);
        }
    }
    private void DeleteSingleFile(string target, DelStats del)
    {
        try
        {
            if (File.Exists(target))
            {
                File.SetAttributes(target, FileAttributes.Normal);
                File.Delete(target);
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
    private static IEnumerable<string> OrderDirsByDepthDesc(IEnumerable<string> dirsToDelete) =>
        dirsToDelete.OrderByDescending(d => d.Count(ch => ch == Path.DirectorySeparatorChar));
    private void DeleteDirectories(string root, IEnumerable<string> dirsToDelete, DelStats del, CancellationToken ct)
    {
        foreach (var relDir in OrderDirsByDepthDesc(dirsToDelete))
        {
            ct.ThrowIfCancellationRequested();
            DeleteDirectoryIfEmpty(root, relDir, del);
        }
    }
    private void DeleteDirectoryIfEmpty(string root, string relDir, DelStats del)
    {
        var target = Path.Combine(root, relDir);

        try
        {
            if (Directory.Exists(target) && IsDirectoryEmpty(target))
            {
                TryUnsetReadOnly(target);
                Directory.Delete(target, recursive: false);
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
    private void TryUnsetReadOnly(string path)
    {
        var attr = File.GetAttributes(path);
        try
        {
            if (attr.HasFlag(FileAttributes.ReadOnly))
            {
                File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogWarning("Failed to unset read only for file {File}", path);
            logger.LogDebug(ex, "Failed to unset read only for file {File}", path);
        }
    }
    private static bool IsDirectoryEmpty(string path)
    {
        try
        {
            return !Directory.EnumerateDirectories(path).Any();
        }
        catch
        {
            return false;
        }
    }
}