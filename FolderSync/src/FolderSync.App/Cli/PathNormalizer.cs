using FolderSync.App.Cli.Interfaces;

namespace FolderSync.App.Cli;

public class PathNormalizer : IPathNormalizer
{
    public string NormalizeExistingDirectory(string path, bool mustExist, string name)
    {
        var full = Path.GetFullPath(path);
        if (mustExist && !Directory.Exists(full))
            throw new ArgumentException($"Path {name} doesn't exist: {full}");
        return TrimEndingSeparators(full);
    }

    public string NormalizeDirectory(string path, string name)
    {
        try
        {
            var full = Path.GetFullPath(path);
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
            var full = Path.GetFullPath(path);
            var dir = Path.GetDirectoryName(full);
            if (string.IsNullOrWhiteSpace(dir))
                throw new ArgumentException($"{name}: The file path must include a parent directory");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return full;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Incorrect path for {name}: {path}. {ex.Message}");
        }
    }

    private static string TrimEndingSeparators(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}