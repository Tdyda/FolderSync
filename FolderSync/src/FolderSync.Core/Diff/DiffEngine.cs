using FolderSync.Core.Abstractions;
using FolderSync.Core.Scanning;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Diff;

public class DiffEngine
{
    private readonly ILogger _logger;
    public DiffEngine(ILogger logger) => _logger = logger;

    public DiffResult Compute(DirectorySnapshot source, DirectorySnapshot replica)
    {
        var comparer = PathComparer.ForPaths;
        var dirsToCreate = ComputeDirsToCreate(source, replica, comparer);
        var dirsToDelete = ComputeDirsToDelete(source, replica, comparer);
        var filesToCopy = ComputeFilesToCopy(source, replica, comparer);
        var filesToUpdate = ComputeFilesToUpdate(source, replica, comparer);
        var filesToDelete = ComputeFilesToDelete(source, replica, comparer);

        var result = new DiffResult
        {
            DirsToCreate = dirsToCreate,
            DirsToDelete = dirsToDelete,
            FilesToCopy = filesToCopy,
            FilesToDelete = filesToDelete,
            FilesToUpdate = filesToUpdate
        };

        _logger.LogInformation(
            "Diff: Dirs to create: {Create}, dirs to delete: {DeleteDirs}, files to copy: {Copy}, files to update: {Update}, files to delete: {DeleteFiles}",
            result.DirsToCreate.Count, result.DirsToDelete.Count, result.FilesToCopy.Count, result.FilesToUpdate.Count,
            result.FilesToDelete.Count);
        return result;
    }

    private static HashSet<string> ComputeDirsToCreate(DirectorySnapshot source, DirectorySnapshot replica,
        IEqualityComparer<string> cmp) =>
        new HashSet<string>(source.Directories.Where(d => !string.IsNullOrEmpty(d)).Except(replica.Directories, cmp),
            cmp);

    private static HashSet<string> ComputeDirsToDelete(DirectorySnapshot source, DirectorySnapshot replica,
        IEqualityComparer<string> cmp) =>
        new HashSet<string>(replica.Directories.Where(d => !string.IsNullOrEmpty(d)).Except(source.Directories, cmp),
            cmp);

    private static HashSet<string> ComputeFilesToCopy(DirectorySnapshot source, DirectorySnapshot replica,
        IEqualityComparer<string> cmp) =>
        new HashSet<string>(source.Files.Keys.Except(replica.Files.Keys, cmp), cmp);

    private static HashSet<string> ComputeFilesToDelete(DirectorySnapshot source, DirectorySnapshot replica,
        IEqualityComparer<string> cmp) =>
        new HashSet<string>(replica.Files.Keys.Except(source.Files.Keys, cmp), cmp);

    private static HashSet<string> ComputeFilesToUpdate(DirectorySnapshot source, DirectorySnapshot replica,
        IEqualityComparer<string> cmp) =>
        source.Files.Keys.Intersect(replica.Files.Keys, cmp).Where(d => !Same(source.Files[d], replica.Files[d]))
            .ToHashSet(cmp);


    private static bool Same(FileMetadata a, FileMetadata b)
    {
        return a.Size == b.Size && a.LastWriteTimeUtc == b.LastWriteTimeUtc;
    }
}