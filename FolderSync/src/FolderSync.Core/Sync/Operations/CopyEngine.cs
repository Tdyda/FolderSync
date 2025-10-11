using System.IO.Abstractions;
using FolderSync.Core.Operations;
using FolderSync.Core.Results;
using FolderSync.Core.Sync.Scanning;
using FolderSync.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Sync.Operations;

public class CopyEngine(ILogger<CopyEngine> logger, IFileSystem fs, IFileOps fileOps)
{
    public async Task<CopyStats> ExecAsync(DirectorySnapshot source, DirectorySnapshot replica, DiffResult diff,
        CancellationToken ct = default)
    {
        var copy = new CopyStats();

        CreateDirectories(diff.DirsToCreate, replica.RootPath, copy, ct);
        await SyncFilesAsync(diff.FilesToCopy, source.RootPath, replica.RootPath, source.Files, copy,
            s => s.FilesCopied++, ct);
        await SyncFilesAsync(diff.FilesToUpdate, source.RootPath, replica.RootPath, source.Files, copy,
            s => s.FilesUpdated++, ct);

        return copy;
    }

    private void CreateDirectories(IEnumerable<string> dirsToCreate, string root, CopyStats copy,
        CancellationToken ct)
    {
        foreach (var relDir in dirsToCreate)
        {
            ct.ThrowIfCancellationRequested();
            var targetDir = fs.Path.Combine(root, relDir);
            try
            {
                fs.Directory.CreateDirectory(targetDir);
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

    // Considered using a record (data class) for the parameters, but for this small project
    // introducing an extra file/type would add overhead with little benefit.
    private async Task SyncFilesAsync(IEnumerable<string> relFiles, string rootSrc, string rootDst,
        IReadOnlyDictionary<string, FileMetadata> info, CopyStats stats, Action<CopyStats> bump,
        CancellationToken ct)
    {
        foreach (var rel in relFiles)
        {
            ct.ThrowIfCancellationRequested();

            var src = fs.Path.Combine(rootSrc, rel);
            var dst = fs.Path.Combine(rootDst, rel);

            try
            {
                await fileOps.AtomicCopyAsync(src, dst, ct).ConfigureAwait(false);
                fs.File.SetLastWriteTimeUtc(dst, info[rel].LastWriteTimeUtc);

                var beforeCopied = stats.FilesCopied;
                bump(stats);
                var verb = stats.FilesCopied > beforeCopied ? "Copied" : "Updated";

                logger.LogInformation("{Verb}: {Rel} -> {Dst}", verb, rel, dst);
            }
            catch (Exception ex) when (ex.IsBenign())
            {
                logger.LogError("Failed to process file: {Src} -> {Dst}: {Error}", src, dst, ex.Message);
                logger.LogDebug(ex, "Stacktrace: ");
            }
        }
    }
}