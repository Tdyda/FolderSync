using System.IO.Abstractions.TestingHelpers;
using FolderSync.Core.Sync.Operations;

namespace FolderSync.Core.Tests;

public class FileCopierTests
{
    private static string J(MockFileSystem fs, params string[] parts)
    {
        var p = parts[0];
        for (var i = 1; i < parts.Length; i++)
            p = fs.Path.Combine(p, parts[i]);
        return p;
    }

    [Fact]
    public async Task AtomicCopyAsync_MovesTemp_WhenDestinationDoesNotExist()
    {
        var fs = new MockFileSystem();
        var copier = new FileCopier(fs);

        var srcRoot = "src";
        var dstRoot = "dst";
        fs.AddDirectory(srcRoot);
        fs.AddDirectory(dstRoot);

        fs.AddFile(J(fs, srcRoot, "a.txt"), new MockFileData("HELLO"));

        var src = J(fs, srcRoot, "a.txt");
        var dst = J(fs, dstRoot, "a.txt");

        await copier.AtomicCopyAsync(src, dst, CancellationToken.None);

        Assert.True(fs.FileExists(dst));
        Assert.Equal("HELLO", await fs.File.ReadAllTextAsync(dst));

        var tempLeftovers = fs.AllFiles.Where(p => p.Contains(".fs_temp", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(tempLeftovers);
    }

    [Fact]
    public async Task AtomicCopyAsync_Replaces_WhenDestinationExists()
    {
        var fs = new MockFileSystem();
        var copier = new FileCopier(fs);

        var srcRoot = "src";
        var dstRoot = "dst";
        fs.AddDirectory(srcRoot);
        fs.AddDirectory(dstRoot);

        fs.AddFile(J(fs, srcRoot, "data.bin"), new MockFileData("NEW"));
        fs.AddFile(J(fs, dstRoot, "data.bin"), new MockFileData("OLD"));

        var src = J(fs, srcRoot, "data.bin");
        var dst = J(fs, dstRoot, "data.bin");

        await copier.AtomicCopyAsync(src, dst, CancellationToken.None);

        Assert.True(fs.FileExists(dst));
        Assert.Equal("NEW", await fs.File.ReadAllTextAsync(dst));

        var tempLeftovers = fs.AllFiles.Where(p => p.Contains(".fs_temp", StringComparison.OrdinalIgnoreCase));
        Assert.Empty(tempLeftovers);
    }

    [Fact]
    public async Task AtomicCopyAsync_CreatesDestinationDirectory_IfMissing()
    {
        var fs = new MockFileSystem();
        var copier = new FileCopier(fs);

        var srcRoot = "src";
        var dstRoot = "dst";
        fs.AddDirectory(srcRoot);
        fs.AddDirectory(dstRoot);

        fs.AddFile(J(fs, srcRoot, "deep.txt"), new MockFileData("X"));

        var src = J(fs, srcRoot, "deep.txt");
        var dst = J(fs, dstRoot, "level1", "level2", "deep.txt");

        await copier.AtomicCopyAsync(src, dst, CancellationToken.None);

        Assert.True(fs.Directory.Exists(J(fs, dstRoot, "level1", "level2")));
        Assert.True(fs.FileExists(dst));
        Assert.Equal("X", await fs.File.ReadAllTextAsync(dst));
    }

    [Fact]
    public async Task AtomicCopyAsync_Respects_CancellationToken()
    {
        var fs = new MockFileSystem();
        var copier = new FileCopier(fs);

        var srcRoot = "src";
        var dstRoot = "dst";
        fs.AddDirectory(srcRoot);
        fs.AddDirectory(dstRoot);

        fs.AddFile(fs.Path.Combine(srcRoot, "big.dat"), new MockFileData(new string('A', 1024 * 64)));

        var src = fs.Path.Combine(srcRoot, "big.dat");
        var dst = fs.Path.Combine(dstRoot, "big.dat");

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            copier.AtomicCopyAsync(src, dst, cts.Token));
    }
}