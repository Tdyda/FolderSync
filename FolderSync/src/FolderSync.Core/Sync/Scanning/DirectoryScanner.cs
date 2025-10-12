using System.IO.Abstractions;
using FolderSync.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Sync.Scanning;

public sealed class DirectoryScanner(ILogger<DirectoryScanner> logger, IFileSystem fs)
{
    public Task<DirectorySnapshot> BuildSnapshotAsync(string rootPath, CancellationToken ct = default)
    {
        if (!fs.Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Directory not found: {rootPath}");

        var files = new Dictionary<string, FileMetadata>(PathComparer.ForPaths);
        var dirs = new HashSet<string>(PathComparer.ForPaths) { string.Empty };

        var stack = new Stack<string>();
        stack.Push(rootPath);

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var current = stack.Pop();

            CollectChildDirectories(stack, dirs, current, rootPath);
            CollectFileMetadataInDirectory(files, current, rootPath);
        }

        var snapshot = new DirectorySnapshot
        {
            RootPath = rootPath,
            Files = files,
            Directories = dirs
        };

        logger.LogDebug("Built {Snapshot}", snapshot);
        return Task.FromResult(snapshot);
    }

    private void CollectChildDirectories(Stack<string> stack, HashSet<string> dirs, string currentDir, string rootPath)
    {
        try
        {
            foreach (var dir in fs.Directory.EnumerateDirectories(currentDir))
            {
                var di = fs.DirectoryInfo.New(dir);
                if (di.Attributes.HasFlag(FileAttributes.ReparsePoint) || !string.IsNullOrEmpty(di.LinkTarget))
                {
                    logger.LogDebug("Skipping reparse point: {Dir}", dir);
                    continue;
                }

                var relDir = ToRelative(rootPath, dir);
                dirs.Add(relDir);
                stack.Push(dir);
            }
        }
        catch (Exception ex) when (ex.IsBenign())
        {
            logger.LogWarning("Failed to enumerate directories in {Current}: {Error}", currentDir, ex.Message);
            logger.LogDebug(ex, "Stacktrace: ");
        }
    }

    // NOTE: Considered an IFileMetadataProvider for pluggable metadata,
    // but skipped due to stable requirements (size + LastWriteTimeUtc).
    // Refactor here if this policy ever changes.
    private void CollectFileMetadataInDirectory(Dictionary<string, FileMetadata> files, string currentDir,
        string rootPath)
    {
        try
        {
            foreach (var file in fs.Directory.EnumerateFiles(currentDir))
                try
                {
                    var fi = fs.FileInfo.New(file);

                    if (fi.Attributes.HasFlag(FileAttributes.ReparsePoint) || !string.IsNullOrEmpty(fi.LinkTarget))
                    {
                        logger.LogDebug("Skipping reparse point: {File}", file);
                        continue;
                    }

                    var meta = new FileMetadata(fi.Length, fi.LastWriteTimeUtc);
                    var relFile = ToRelative(rootPath, file);
                    files[relFile] = meta;
                }
                catch (Exception ex) when (ex.IsBenign())
                {
                    logger.LogWarning("Failed to read file metadata: {File}: {Error}", file, ex.Message);
                    logger.LogDebug(ex, "Stacktrace: ");
                }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Failed to enumerate files in {Current}: {Error}", currentDir, ex.Message);
            logger.LogDebug(ex, "Stacktrace: ");
        }
    }

    private string ToRelative(string root, string fullPath)
    {
        var rel = fs.Path.GetRelativePath(root, fullPath);

        rel = rel.Replace(fs.Path.AltDirectorySeparatorChar, fs.Path.DirectorySeparatorChar)
            .TrimEnd(fs.Path.DirectorySeparatorChar);
        return rel;
    }
}