using FolderSync.Core.Abstractions;
using FolderSync.Core.Diff;
using FolderSync.Core.Scanning;
using FolderSync.Core.Scheduling;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Copying;

public class CopyEngine
{
    private readonly ILogger<CopyEngine> _logger;
    private const string TempSuffix = ".fs_temp";

    public CopyEngine(ILogger<CopyEngine> logger) => _logger = logger;

    public async Task<CopyStats> ApplyAsync(DirectorySnapshot source, DirectorySnapshot replica, DiffResult diff,
        CancellationToken ct = default)
    {
        var copy = new CopyStats();

        CreateDirectories(diff.DirsToCreate, replica.RootPath, copy, ct);
        await CopyFiles(diff.FilesToCopy, source.RootPath, replica.RootPath, source.Files, copy, ct).ConfigureAwait(false);
        await UpdateFiles(diff.FilesToUpdate, source.RootPath, replica.RootPath, source.Files, copy, ct).ConfigureAwait(false);

        return copy;
    }

    private void CreateDirectories(IEnumerable<string> dirsToCreate, string root, CopyStats copy,
        CancellationToken ct)
    {
        foreach (var relDir in dirsToCreate)
        {
            ct.ThrowIfCancellationRequested();
            CreateSingleDirectory(root, relDir, copy);
        }
    }

    private void CreateSingleDirectory(string root, string relDir, CopyStats copy)
    {
        var targetDir = PathHelpers.CombineUnderRoot(root, relDir);

        try
        {
            Directory.CreateDirectory(targetDir);
            copy.DirsCreated++;
            _logger.LogInformation("Created directory: {Dir}", targetDir);
        }
        catch (Exception ex) when (IsBenignIo(ex))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogWarning(ex, "Failed to create directory: {Dir}", targetDir);
            _logger.LogDebug(ex, "Failed to create directory: {Dir}", targetDir);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogWarning("Unexpected error: {Msg}", ex.Message);
            _logger.LogDebug(ex, "Unexpected error");
        }
    }

    private async Task CopyFiles(IEnumerable<string> filesToCopy, string rootSrc, string rootDst,
        IReadOnlyDictionary<string, FileMetadata> info, CopyStats copy, CancellationToken ct)
    {
        foreach (var relFile in filesToCopy)
        {
            ct.ThrowIfCancellationRequested();
            await CopySingleFile(rootSrc, rootDst, relFile, info, copy, ct).ConfigureAwait(false);
        }
    }

    private async Task CopySingleFile(string rootSrc, string rootDst, string relFile,
        IReadOnlyDictionary<string, FileMetadata> info, CopyStats copy, CancellationToken ct)
    {
        var src = PathHelpers.CombineUnderRoot(rootSrc, relFile);
        var dst = PathHelpers.CombineUnderRoot(rootDst, relFile);

        try
        {
            await AtomicCopyAsync(src, dst, ct).ConfigureAwait(false);
            var ts = info[relFile].LastWriteTimeUtc;
            File.SetLastWriteTimeUtc(dst, ts);
            copy.FilesCopied++;
            _logger.LogInformation("Copied: {Rel} -> {Dst}", relFile, dst);
        }
        catch (Exception ex) when (IsBenignIo(ex))
        {
            _logger.LogError(ex, "Failed to copy file: {Src} -> {Dst}", src, dst);
        }
    }

    private static async Task AtomicCopyAsync(string sourceFile, string destinationFile, CancellationToken ct)
    {
        PathHelpers.EnsureDirectoryForFile(destinationFile);

        var tempFile = destinationFile + TempSuffix + "." + Guid.NewGuid().ToString("N");

        using (var src = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (var dst = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await src.CopyToAsync(dst, ct).ConfigureAwait(false);
        }

        if (File.Exists(destinationFile))
            File.Replace(tempFile, destinationFile, destinationBackupFileName: null, ignoreMetadataErrors: true);
        else
            File.Move(tempFile, destinationFile);
    }

    private async Task UpdateFiles(IEnumerable<string> filesToUpdate, string rootSrc, string rootDst,
        IReadOnlyDictionary<string, FileMetadata> info, CopyStats copy, CancellationToken ct)
    {
        foreach (var relFile in filesToUpdate)
        {
            ct.ThrowIfCancellationRequested();
            await UpdateSingleFile(rootSrc, rootDst, relFile, info, copy, ct).ConfigureAwait(false);
        }
    }

    private async Task UpdateSingleFile(string rootSrc, string rootDst, string relFile,
        IReadOnlyDictionary<string, FileMetadata> info, CopyStats copy, CancellationToken ct)
    {
        var src = PathHelpers.CombineUnderRoot(rootSrc, relFile);
        var dst = PathHelpers.CombineUnderRoot(rootDst, relFile);
        try
        {
            await AtomicCopyAsync(src, dst, ct).ConfigureAwait(false);
            var ts = info[relFile].LastWriteTimeUtc;
            File.SetLastWriteTimeUtc(dst, ts);
            copy.FilesCopied++;
            _logger.LogInformation("Updated: {Rel} -> {Dst}", relFile, dst);
        }
        catch (Exception ex) when (IsBenignIo(ex))
        {
            _logger.LogError(ex, "Failed to update file: {Src} -> {Dst}", src, dst);
        }
    }

    private static bool IsBenignIo(Exception ex) => ex is IOException
                                                    || ex is UnauthorizedAccessException
                                                    || ex is DirectoryNotFoundException
                                                    || ex is FileNotFoundException
                                                    || ex is PathTooLongException;
}