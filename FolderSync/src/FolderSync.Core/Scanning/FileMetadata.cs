namespace FolderSync.Core.Scanning;

public readonly record struct FileMetadata(long Size, DateTime LastWriteTimeUtc);