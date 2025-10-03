namespace FolderSync.Core.Abstractions;

public static class PathComparer
{
    public static StringComparer ForPaths => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
}