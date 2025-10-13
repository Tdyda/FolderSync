namespace FolderSync.Core.Sync.Scanning;

public readonly record struct FileMetadata(long Size, DateTime LastWriteTimeUtc);