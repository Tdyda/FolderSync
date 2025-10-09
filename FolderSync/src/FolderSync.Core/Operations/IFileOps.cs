namespace FolderSync.Core.Operations;

public interface IFileOps
{
    Task AtomicCopyAsync(string sourceFile, string destinationFile, CancellationToken ct);
}