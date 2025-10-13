using System.IO.Abstractions;
using FolderSync.App.Interfaces;

namespace FolderSync.App.Cli;

public class PathNormalizer(IFileSystem fs) : IPathNormalizer
{
    public string NormalizeExistingDirectory(string path, bool mustExist, string name)
    {
        var full = fs.Path.GetFullPath(path);
        if (mustExist && !fs.Directory.Exists(full))
            throw new ArgumentException($"Path {name} doesn't exist: {full}");
        return TrimEndingSeparators(full);
    }

    public string NormalizeDirectory(string path, string name)
    {
        try
        {
            var full = fs.Path.GetFullPath(path);
            return TrimEndingSeparators(full);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Incorrect path for {name}: {path}. {ex.Message}");
        }
    }

    public string NormalizeFilePath(string path, string name)
    {
        try
        {
            var full = fs.Path.GetFullPath(path);
            var dir = fs.Path.GetDirectoryName(full)!;

            if (!fs.Directory.Exists(dir)) fs.Directory.CreateDirectory(dir);
            return full;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Incorrect path for {name}: {path}. {ex.Message}");
        }
    }

    private string TrimEndingSeparators(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        return path.TrimEnd(fs.Path.DirectorySeparatorChar, fs.Path.AltDirectorySeparatorChar);
    }
}