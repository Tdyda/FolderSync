namespace FolderSync.Core.Interfaces;

public interface IFileCopier
{
    Task AtomicCopyAsync(string source, string destination, CancellationToken ct);
}