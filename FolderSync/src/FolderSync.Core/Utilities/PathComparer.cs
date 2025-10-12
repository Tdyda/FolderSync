namespace FolderSync.Core.Utilities;

public static class PathComparer
{
    public static StringComparer ForPaths =>
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
}