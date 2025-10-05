using FolderSync.Core.Abstractions;
using FolderSync.Core.Diff;
using FolderSync.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Deletion;

public class DeletionEngine
{
    private readonly ILogger<DeletionEngine> _logger;

    public DeletionEngine(ILogger<DeletionEngine> logger) => _logger = logger;

    public Task DeleteAsync(DirectorySnapshot source, DirectorySnapshot replica, DiffResult diff,
        CancellationToken ct = default)
    {
        var filesToDelete = diff.FilesToDelete;
        var dirsToDelete = diff.DirsToDelete;

        foreach (var relFile in filesToDelete)
        {
            ct.ThrowIfCancellationRequested();
            var target = PathHelpers.CombineUnderRoot(replica.RootPath, relFile);

            try
            {
                if (File.Exists(target))
                {
                    File.SetAttributes(target, FileAttributes.Normal);
                    File.Delete(target);
                    _logger.LogInformation("Deleted file {File}", target);
                }
            }
            catch (Exception ex) when (IsBenignIo(ex))
            {
                _logger.LogWarning("Failed to delete file {File}", target);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error: {Error}", ex.Message);
            }
        }

        var dirsByDepth = diff.DirsToDelete
            .OrderByDescending(d => d.Count(ch => ch == Path.DirectorySeparatorChar));

        foreach (var relDir in dirsByDepth)
        {
            ct.ThrowIfCancellationRequested();
            var target = PathHelpers.CombineUnderRoot(replica.RootPath, relDir);

            try
            {
                if (Directory.Exists(target) && IsDirectoryEmpty(target))
                {
                    TryUnsetReadOnly(target);
                    Directory.Delete(target, recursive: false);
                    _logger.LogInformation("Deleted directory {Dir}", target);
                }
            }
            catch (Exception ex) when (IsBenignIo(ex))
            {
                _logger.LogWarning("Failed to delete directory {Dir}, {Ex}", target, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error: {Error}", ex.Message);
            }
        }

        return Task.CompletedTask;
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
        catch
        {
            _logger.LogWarning("Failed to unset read only for file {File}",  path);
        }
    }

    private bool IsDirectoryEmpty(string path)
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