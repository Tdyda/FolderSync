using FolderSync.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace FolderSync.Core.Scanning;

public sealed class DirectoryScanner : IDirectoryScanner
{
    private readonly ILogger<DirectoryScanner> _logger;

    public DirectoryScanner(ILogger<DirectoryScanner> logger) => _logger = logger;

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

            try
            {
                foreach (var dir in Directory.EnumerateDirectories(current))
                {
                    if (IsReparsePoint(dir))
                    {
                        _logger.LogDebug($"Skipping reparse point: {dir}");
                        continue;
                    }

                    var relDir = ToRelative(rootPath, dir);
                    dirs.Add(relDir);
                    stack.Push(dir);
                }
            }
            catch (Exception ex) when (IsBenignIo(ex))
            {
                _logger.LogWarning(ex, $"Failed to enumerate directories in {current}");
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        var meta = new FileMetadata(Size: fi.Exists ? fi.Length : 0L,
                            LastWriteTimeUtc: DateTime.MinValue);
                        var relFile = ToRelative(rootPath, file);
                        files[relFile] = meta;
                    }
                    catch (Exception ex) when (IsBenignIo(ex))
                    {
                        _logger.LogWarning(ex, $"Failed to read file metadata: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to enumerate files in {current}");
            }
        }

        var snapshot = new DirectorySnapshot
        {
            RootPath = rootPath,
            Files = files,
            Directories = dirs,
        };

        _logger.LogInformation($"Built {snapshot}");
        return Task.FromResult(snapshot);
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

    private static bool IsBenignIo(Exception ex) => ex is UnauthorizedAccessException
                                                    || ex is IOException
                                                    || ex is DirectoryNotFoundException
                                                    || ex is FileNotFoundException
                                                    || ex is PathTooLongException;
}