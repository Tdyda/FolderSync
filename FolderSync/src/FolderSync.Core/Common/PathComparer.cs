namespace FolderSync.Core.Common;

public static class PathComparer
{
    public static StringComparer ForPaths => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}