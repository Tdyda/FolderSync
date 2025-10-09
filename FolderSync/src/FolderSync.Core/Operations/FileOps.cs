using System.IO.Abstractions;

namespace FolderSync.Core.Operations;

public class FileOps : IFileOps
{
    private const string TempSuffix = ".fs_temp";
    private readonly IFileSystem _fs;

    public FileOps(IFileSystem fs) => _fs = fs;

    public async Task AtomicCopyAsync(string sourceFile, string destinationFile, CancellationToken ct)
    {
        var dir = _fs.Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrEmpty(dir))
            _fs.Directory.CreateDirectory(dir);

        var tempFile = $"{destinationFile}{TempSuffix}.{Guid.NewGuid():N}";

        await using (var src = _fs.File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var dst = _fs.File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await src.CopyToAsync(dst, ct).ConfigureAwait(false);
        }

        if (_fs.File.Exists(destinationFile))
            _fs.File.Replace(tempFile, destinationFile, destinationBackupFileName: null, ignoreMetadataErrors: true);
        else
            _fs.File.Move(tempFile, destinationFile);
    }
}