using FolderSync.Core.Common;
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
            var target = PathHelpers.CombineUnderRoot(root, relFile);
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
                _logger.LogInformation("Deleted file {File}", target);
            }
        }
        catch (Exception ex) when (IsBenignIo(ex))
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
                _logger.LogWarning("Failed to delete file {File}: {Error}", target, ex.Message);
            _logger.LogDebug(ex, "Failed to delete file {File}", target);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
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
        var target = PathHelpers.CombineUnderRoot(root, relDir);

        try
        {
            if (Directory.Exists(target) && IsDirectoryEmpty(target))
            {
                TryUnsetReadOnly(target);
                Directory.Delete(target, recursive: false);
                del.DirsDeleted++;
                _logger.LogInformation("Deleted directory {Dir}", target);
            }
        }
        catch (Exception ex) when (IsBenignIo(ex))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogWarning("Failed to delete directory {Dir}: {Error}", target, ex.Message);
            _logger.LogDebug(ex, "Failed to delete directory {Dir}", target);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error");
            _logger.LogDebug(ex, "Unexpected error");
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
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogWarning("Failed to unset read only for file {File}", path);
            _logger.LogDebug(ex, "Failed to unset read only for file {File}", path);
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
    private static bool IsBenignIo(Exception ex) => ex is IOException
                                                    || ex is UnauthorizedAccessException
                                                    || ex is DirectoryNotFoundException
                                                    || ex is FileNotFoundException
                                                    || ex is PathTooLongException;
}