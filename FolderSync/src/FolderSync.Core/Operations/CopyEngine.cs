using FolderSync.Core.Common;
using FolderSync.Core.Extensions;
using FolderSync.Core.Results;
using FolderSync.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Operations;

public class CopyEngine(ILogger<CopyEngine> logger)
{
    private const string TempSuffix = ".fs_temp";

    public async Task<CopyStats> ApplyAsync(DirectorySnapshot source, DirectorySnapshot replica, DiffResult diff,
        CancellationToken ct = default)
    {
        var copy = new CopyStats();

        CreateDirectories(diff.DirsToCreate, replica.RootPath, copy, ct);
        await SyncFilesAsync(diff.FilesToCopy, source.RootPath, replica.RootPath, source.Files, copy, SyncMode.Copy,
            ct);
        await SyncFilesAsync(diff.FilesToUpdate, source.RootPath, replica.RootPath, source.Files, copy, SyncMode.Update,
            ct);

        return copy;
    }

    private void CreateDirectories(IEnumerable<string> dirsToCreate, string root, CopyStats copy,
        CancellationToken ct)
    {
        foreach (var relDir in dirsToCreate)
        {
            ct.ThrowIfCancellationRequested();
            var targetDir = Path.Combine(root, relDir);
            try
            {
                Directory.CreateDirectory(targetDir);
                copy.DirsCreated++;
                logger.LogInformation("Created directory: {Dir}", targetDir);
            }
            catch (Exception ex) when (ex.IsBenign())
            {
                logger.LogWarning("Failed to create directory: {Dir}", targetDir);
                logger.LogDebug(ex, "Failed to create directory: {Dir}", targetDir);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Unexpected error: {Msg}", ex.Message);
                logger.LogDebug(ex, "Unexpected error");
            }
        }
    }

    private enum SyncMode
    {
        Copy,
        Update
    }

    private async Task SyncFilesAsync(IEnumerable<string> relFiles, string rootSrc, string rootDst,
        IReadOnlyDictionary<string, FileMetadata> info, CopyStats stats, SyncMode mode, CancellationToken ct)
    {
        foreach (var rel in relFiles)
        {
            ct.ThrowIfCancellationRequested();

            var src = Path.Combine(rootSrc, rel);
            var dst = Path.Combine(rootDst, rel);

            try
            {
                await FileOps.AtomicCopyAsync(src, dst, ct).ConfigureAwait(false);
                File.SetLastWriteTimeUtc(dst, info[rel].LastWriteTimeUtc);

                if (mode == SyncMode.Copy)
                {
                    stats.FilesCopied++;
                    logger.LogInformation("Copied: {Rel} -> {Dst}", rel, dst);
                }
                else
                {
                    stats.FilesUpdated++;
                    logger.LogInformation("Updated: {Rel} -> {Dst}", rel, dst);
                }
            }
            catch (Exception ex) when (ex.IsBenign())
            {
                var verb = mode == SyncMode.Copy ? "copy" : "update";
                logger.LogError("Failed to {Verb} file: {Src} -> {Dst}: {Error}", verb, src, dst, ex.Message);
                logger.LogDebug(ex, "Stacktrace: ");
            }
        }
    }
}

internal static class FileOps
{
    private const string TempSuffix = ".fs_temp";

    public static async Task AtomicCopyAsync(string sourceFile, string destinationFile, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var tempFile = $"{destinationFile}{TempSuffix}.{Guid.NewGuid():N}";

        await using (var src = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var dst = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await src.CopyToAsync(dst, ct).ConfigureAwait(false);
        }

        if (File.Exists(destinationFile))
            File.Replace(tempFile, destinationFile, destinationBackupFileName: null, ignoreMetadataErrors: true);
        else
            File.Move(tempFile, destinationFile);
    }
}