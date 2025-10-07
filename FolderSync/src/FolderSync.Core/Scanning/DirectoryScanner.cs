using FolderSync.Core.Common;
using FolderSync.Core.Extensions;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Scanning;

public sealed class DirectoryScanner(ILogger<DirectoryScanner> logger) : IDirectoryScanner
{
    public Task<DirectorySnapshot> BuildSnapshotAsync(string rootPath, CancellationToken ct = default)
    {
        if (!Directory.Exists(rootPath))
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
            Directories = dirs,
        };

        logger.LogInformation("Built {Snapshot}", snapshot);
        return Task.FromResult(snapshot);
    }

    private void CollectChildDirectories(Stack<string> stack, HashSet<string> dirs, string currentDir, string rootPath)
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(currentDir))
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
            if (!logger.IsEnabled(LogLevel.Error))
                logger.LogWarning("Failed to enumerate directories in {Current}: {Error}", currentDir, ex.Message);
            logger.LogDebug(ex, "Failed to enumerate directories in {Dir}", currentDir);
        }
    }

    private void CollectFileMetadataInDirectory(Dictionary<string, FileMetadata> files, string currentDir,
        string rootPath)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(currentDir))
            {
                try
                {
                    var fi = new FileInfo(file);
                    var meta = new FileMetadata(Size: fi.Exists ? fi.Length : 0L,
                        LastWriteTimeUtc: fi.Exists ? fi.LastWriteTimeUtc : DateTime.MinValue);
                    var relFile = ToRelative(rootPath, file);
                    files[relFile] = meta;
                }
                catch (Exception ex) when (ex.IsBenign())
                {
                    if (!logger.IsEnabled(LogLevel.Debug))
                        logger.LogWarning("Failed to read file metadata: {File}: {Error}", file, ex.Message);
                    logger.LogDebug(ex, "Failed to read file metadata: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                logger.LogWarning("Failed to enumerate files in {Current}: {Error}", currentDir, ex.Message);
            logger.LogDebug(ex, "Failed to enumerate files in {Current}", currentDir);
        }
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            var attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.ReparsePoint);
        }
        catch
        {
            return false;
        }
    }

    private static string ToRelative(string root, string fullPath)
    {
        var rel = Path.GetRelativePath(root, fullPath);

        rel = rel.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar);
        return rel;
    }
}