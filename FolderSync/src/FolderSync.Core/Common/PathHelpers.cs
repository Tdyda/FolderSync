namespace FolderSync.Core.Common;

public static class PathHelpers
{
    public static string CombineUnderRoot(string root, string relative)
    {
        return Path.Combine(root, relative);
    }

    public static void EnsureDirectoryForFile(string fullFilePath)
    {
        var dir = Path.GetDirectoryName(fullFilePath);
        if(!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }
}