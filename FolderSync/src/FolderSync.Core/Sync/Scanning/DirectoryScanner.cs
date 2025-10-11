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

        logger.LogInformation("Built {Snapshot}", snapshot);
        return Task.FromResult(snapshot);
    }

    private void CollectChildDirectories(Stack<string> stack, HashSet<string> dirs, string currentDir, string rootPath)
    {
        try
        {
            foreach (var dir in fs.Directory.EnumerateDirectories(currentDir))
            {
                if (IsReparsePoint(dir))
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
                    var meta = new FileMetadata(fi.Exists ? fi.Length : 0L,
                        fi.Exists ? fi.LastWriteTimeUtc : DateTime.MinValue);
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

    private bool IsReparsePoint(string path)
    {
        try
        {
            var attr = fs.File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false;
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