using FolderSync.Core.Abstractions;
using FolderSync.Core.Scanning;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;

namespace FolderSync.Core.Diff;

public class DiffEngine
{
    private readonly ILogger<DiffEngine> _logger;

    public DiffEngine(ILogger<DiffEngine> logger) => _logger = logger;

    public DiffResult Compute(DirectorySnapshot source, DirectorySnapshot replica)
    {
        if (source is null) throw new ArgumentException(nameof(source));
        if (replica is null) throw new ArgumentException(nameof(replica));

        var comparer = PathComparer.ForPaths;

        var dirsToCreate = new HashSet<string>(comparer);
        var dirsToDelete = new HashSet<string>(comparer);

        foreach (var dir in source.Directories)
        {
            if(dir.Length == 0) continue;
            if (!replica.Directories.Contains(dir))
                dirsToCreate.Add(dir);
        }

        foreach (var dir in replica.Directories)
        {
            if (dir.Length == 0) continue;
            if (!source.Directories.Contains(dir))
                dirsToDelete.Add(dir);
        }
        
        var filesToCopy = new HashSet<string>(comparer);
        var filesToDelete = new HashSet<string>(comparer);
        var filesToUpdate = new HashSet<string>(comparer);

        foreach (var kv in source.Files)
        {
            var rel = kv.Key;
            var srcMeta = kv.Value;
            if (!replica.Files.TryGetValue(rel, out var repMeta))
            {
                filesToCopy.Add(rel);
            }
            else if (!Same(srcMeta, repMeta))
            {
                filesToUpdate.Add(kv.Key);
            }
        }

        foreach (var rel in replica.Files.Keys)
        {
            if(!source.Files.ContainsKey(rel))
                filesToDelete.Add(rel);
        }

        var result = new DiffResult
        {
            DirsToCreate = dirsToCreate,
            DirsToDelete = dirsToDelete,
            FilesToCopy = filesToCopy,
            FilesToDelete = filesToDelete,
            FilesToUpdate = filesToUpdate
        };
        
        return result;
    }

    private static bool Same(FileMetadata a, FileMetadata b)
    {
        return a.Size == b.Size && a.LastWriteTimeUtc == b.LastWriteTimeUtc;
    }
}