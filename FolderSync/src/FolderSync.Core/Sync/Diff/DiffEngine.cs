using FolderSync.Core.Results;
using FolderSync.Core.Sync.Scanning;
using FolderSync.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Sync.Diff;

public class DiffEngine(ILogger<DiffEngine> logger)
{
    public DiffResult Compute(DirectorySnapshot source, DirectorySnapshot replica)
    {
        var comparer = PathComparer.ForPaths;
        var dirsToCreate = ComputeDirsToCreate(source, replica, comparer);
        var dirsToDelete = ComputeDirsToDelete(source, replica, comparer);
        var filesToCopy = ComputeFileDiff(source.Files, replica.Files, comparer);
        var filesToUpdate = ComputeFilesToUpdate(source, replica, comparer);
        var filesToDelete = ComputeFileDiff(replica.Files, source.Files, comparer);

        var result = new DiffResult
        {
            DirsToCreate = dirsToCreate,
            DirsToDelete = dirsToDelete,
            FilesToCopy = filesToCopy,
            FilesToDelete = filesToDelete,
            FilesToUpdate = filesToUpdate
        };

        logger.LogInformation(
            "Diff: Dirs to create: {Create}, dirs to delete: {DeleteDirs}, files to copy: {Copy}, files to update: {Update}, files to delete: {DeleteFiles}",
            result.DirsToCreate.Count, result.DirsToDelete.Count, result.FilesToCopy.Count, result.FilesToUpdate.Count,
            result.FilesToDelete.Count);
        return result;
    }

    private static HashSet<string> ComputeDirsToCreate(DirectorySnapshot source, DirectorySnapshot replica,
        IEqualityComparer<string> cmp)
    {
        return new HashSet<string>(
            source.Directories.Where(d => !string.IsNullOrEmpty(d)).Except(replica.Directories, cmp),
            cmp);
    }

    private static HashSet<string> ComputeDirsToDelete(DirectorySnapshot source, DirectorySnapshot replica,
        IEqualityComparer<string> cmp)
    {
        return new HashSet<string>(
            replica.Directories.Where(d => !string.IsNullOrEmpty(d)).Except(source.Directories, cmp),
            cmp);
    }

    private static HashSet<string> ComputeFileDiff(IReadOnlyDictionary<string, FileMetadata> left,
        IReadOnlyDictionary<string, FileMetadata> right,
        IEqualityComparer<string> cmp)
    {
        return new HashSet<string>(left.Keys.Except(right.Keys, cmp), cmp);
    }

    private static HashSet<string> ComputeFilesToUpdate(DirectorySnapshot source, DirectorySnapshot replica,
        IEqualityComparer<string> cmp)
    {
        return source.Files.Keys.Intersect(replica.Files.Keys, cmp).Where(d => !Same(source.Files[d], replica.Files[d]))
            .ToHashSet(cmp);
    }


    private static bool Same(FileMetadata a, FileMetadata b)
    {
        if (a.Size == b.Size && a.LastWriteTimeUtc == b.LastWriteTimeUtc) return true;
        if (a.Size != b.Size) return false;

        var diff = (a.LastWriteTimeUtc - b.LastWriteTimeUtc).Duration();
        return diff > TimeSpan.FromSeconds(2);
    }
}