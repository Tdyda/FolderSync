namespace FolderSync.App.Cli;

public static class PathNormalizer
{
    public static string NormalizeExistingDirectory(string path, bool mustExist, string name)
        {
            string full = Path.GetFullPath(path);
            if (mustExist && !Directory.Exists(full))
                throw new ArgumentException($"Path {name} doesn't exits: {full}");
            return TrimEndingSeparators(full);
        }
    public static string NormalizeDirectory(string path, string name)
    {
        try
        {
            string full = Path.GetFullPath(path);
            return TrimEndingSeparators(full);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Incorrect path for {name}: {path}. {ex.Message}");
        }
    }
    public static string NormalizeFilePath(string path, string name)
    {
        try
        {
            string full = Path.GetFullPath(path);
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