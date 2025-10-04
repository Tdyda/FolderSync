using FolderSync.Core.Abstractions;
using FolderSync.Core.Diff;
using FolderSync.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Copying;

public class CopyEngine
{
    private readonly ILogger<CopyEngine> _logger;
    private const string TempSuffix = ".fs_temp";

    public CopyEngine(ILogger<CopyEngine> logger) => _logger = logger;

    public async Task ApplyAsync(DirectorySnapshot source, DirectorySnapshot replica, DiffResult diff,
        CancellationToken ct = default)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (replica is null) throw new ArgumentNullException(nameof(replica));
        if (diff is null) throw new ArgumentNullException(nameof(diff));


        foreach (var relDir in diff.DirsToCreate)
        {
            ct.ThrowIfCancellationRequested();
            var targetDir = PathHelpers.CombineUnderRoot(replica.RootPath, relDir);
            try
            {
                Directory.CreateDirectory(targetDir);
                _logger.LogInformation("Created directory: {Dir}", targetDir);
            }
            catch (Exception ex) when (IsBenignIo(ex))
            {
                _logger.LogWarning(ex, "Failed to create directory: {Dir}", targetDir);
            }
        }

        foreach (var relFile in diff.FilesToCopy)
        {
            ct.ThrowIfCancellationRequested();
            var src = PathHelpers.CombineUnderRoot(source.RootPath, relFile);
            var dst = PathHelpers.CombineUnderRoot(replica.RootPath, relFile);

            try
            {
                await AtomicCopyAsync(src, dst, ct).ConfigureAwait(false);
                var ts = source.Files[relFile].LastWriteTimeUtc;
                File.SetLastWriteTimeUtc(dst, ts);
                _logger.LogInformation("Copied: {Rel} -> {Dst}", relFile, dst);
            }
            catch (Exception ex) when (IsBenignIo(ex))
            {
                _logger.LogError(ex, "Failed to copy file: {Src} -> {Dst}", src, dst);
            }
        }

        foreach (var relFile in diff.FilesToUpdate)
        {
            ct.ThrowIfCancellationRequested();
            var src = PathHelpers.CombineUnderRoot(source.RootPath, relFile);
            var dst = PathHelpers.CombineUnderRoot(replica.RootPath, relFile);
            try
            {
                await AtomicCopyAsync(src, dst, ct).ConfigureAwait(false);
                var ts = source.Files[relFile].LastWriteTimeUtc;
                File.SetLastWriteTimeUtc(dst, ts);
                _logger.LogInformation("Updated: {Rel} -> {Dst}", relFile, dst);
            }
            catch (Exception ex) when (IsBenignIo(ex))
            {
                _logger.LogError(ex, "Failed to update file: {Src} -> {Dst}", src, dst);
            }
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

    private static bool IsBenignIo(Exception ex) => ex is IOException
                                                    || ex is UnauthorizedAccessException
                                                    || ex is DirectoryNotFoundException
                                                    || ex is FileNotFoundException
                                                    || ex is PathTooLongException;
}