namespace FolderSync.Core.Scanning;

public interface IDirectoryScanner
{
    Task<DirectorySnapshot> BuildSnapshotAsync(string rootPath, CancellationToken ct = default);
}