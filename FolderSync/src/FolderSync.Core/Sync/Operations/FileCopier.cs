using System.IO.Abstractions;
using FolderSync.Core.Interfaces;

namespace FolderSync.Core.Sync.Operations;

public class FileCopier : IFileCopier
{
    private const string TempSuffix = ".fs_temp";
    private readonly IFileSystem _fs;

    public FileCopier(IFileSystem fs)
    {
        _fs = fs;
    }

    public async Task AtomicCopyAsync(string source, string destination, CancellationToken ct)
    {
        var dir = _fs.Path.GetDirectoryName(destination);
        if (!string.IsNullOrEmpty(dir))
            _fs.Directory.CreateDirectory(dir);

        var tempFile = $"{destination}{TempSuffix}.{Guid.NewGuid():N}";

        await using (var src = _fs.File.Open(source, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var dst = _fs.File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await src.CopyToAsync(dst, ct).ConfigureAwait(false);
        }

        if (_fs.File.Exists(destination))
            _fs.File.Replace(tempFile, destination, null, true);
        else
            _fs.File.Move(tempFile, destination);
    }
}