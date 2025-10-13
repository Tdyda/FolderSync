using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FolderSync.Core.Interfaces;
using FolderSync.Core.Results;
using FolderSync.Core.Sync.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FolderSync.Core.Tests;

public class CopyEngineTests
{
    private static CopyEngine CreateEngine(IFileSystem fs, IFileCopier copier)
    {
        return new CopyEngine(fs, copier, new NullLogger<CopyEngine>());
    }

    private static string J(MockFileSystem fs, params string[] parts)
    {
        var p = parts[0];
        for (var i = 1; i < parts.Length; i++)
            p = fs.Path.Combine(p, parts[i]);
        return p;
    }

    private static OperationStats Stats()
    {
        return new OperationStats();
    }

    [Fact]
    public async Task Run_Creates_Missing_Directories()
    {
        var fs = new MockFileSystem();
        var src = "src";
        var dst = "dst";
        fs.AddDirectory(src);
        fs.AddDirectory(dst);

        var copier = new Mock<IFileCopier>(MockBehavior.Loose);
        copier.Setup(c => c.AtomicCopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var engine = CreateEngine(fs, copier.Object);

        var toCreate = new[] { "a", "a/b", "c" };
        var stats = await engine.Run(src, dst, Array.Empty<string>(), Array.Empty<string>(), toCreate, Stats(),
            CancellationToken.None);

        Assert.True(fs.Directory.Exists(J(fs, dst, "a")));
        Assert.True(fs.Directory.Exists(J(fs, dst, "a", "b")));
        Assert.True(fs.Directory.Exists(J(fs, dst, "c")));
        Assert.Equal(3, stats.DirsCreated);
    }

    [Fact]
    public async Task Run_DoesNotCreate_Directories_ThatAlreadyExist()
    {
        var fs = new MockFileSystem();
        var src = "src";
        var dst = "dst";
        fs.AddDirectory(src);
        fs.AddDirectory(dst);
        fs.AddDirectory(J(fs, dst, "exists"));

        var copier = new Mock<IFileCopier>(MockBehavior.Loose);
        copier.Setup(c => c.AtomicCopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var engine = CreateEngine(fs, copier.Object);

        var toCreate = new[] { "exists", "new" };
        var stats = await engine.Run(src, dst, Array.Empty<string>(), Array.Empty<string>(), toCreate, Stats(),
            CancellationToken.None);

        Assert.True(fs.Directory.Exists(J(fs, dst, "exists")));
        Assert.True(fs.Directory.Exists(J(fs, dst, "new")));
        Assert.Equal(1, stats.DirsCreated);
    }

    [Fact]
    public async Task Run_Copies_Files_Increments_FilesCopied_Sets_Timestamps()
    {
        var fs = new MockFileSystem();
        var srcRoot = "src";
        var dstRoot = "dst";
        fs.AddDirectory(srcRoot);
        fs.AddDirectory(dstRoot);

        var t1 = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2024, 1, 1, 11, 0, 0, DateTimeKind.Utc);

        fs.AddFile(J(fs, srcRoot, "a.txt"), new MockFileData("A"));
        fs.AddFile(J(fs, srcRoot, "deep", "b.bin"), new MockFileData("BB"));
        fs.File.SetLastWriteTimeUtc(J(fs, srcRoot, "a.txt"), t1);
        fs.File.SetLastWriteTimeUtc(J(fs, srcRoot, "deep", "b.bin"), t2);

        fs.AddDirectory(J(fs, dstRoot, "deep"));
        fs.AddFile(J(fs, dstRoot, "a.txt"), new MockFileData(string.Empty));
        fs.AddFile(J(fs, dstRoot, "deep", "b.bin"), new MockFileData(string.Empty));

        var copier = new Mock<IFileCopier>(MockBehavior.Loose);
        copier.Setup(c => c.AtomicCopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var engine = CreateEngine(fs, copier.Object);

        var stats0 = Stats();
        var stats = await engine.Run(
            srcRoot,
            dstRoot,
            new[] { "a.txt", J(fs, "deep", "b.bin") },
            Array.Empty<string>(),
            Array.Empty<string>(),
            stats0,
            CancellationToken.None);

        Assert.Equal(2, stats.FilesCopied);
        Assert.Equal(0, stats.FilesUpdated);

        Assert.Equal(t1, fs.File.GetLastWriteTimeUtc(J(fs, dstRoot, "a.txt")));
        Assert.Equal(t2, fs.File.GetLastWriteTimeUtc(J(fs, dstRoot, "deep", "b.bin")));

        copier.Verify(
            c => c.AtomicCopyAsync(J(fs, srcRoot, "a.txt"), J(fs, dstRoot, "a.txt"), It.IsAny<CancellationToken>()),
            Times.Once);
        copier.Verify(
            c => c.AtomicCopyAsync(J(fs, srcRoot, "deep", "b.bin"), J(fs, dstRoot, "deep", "b.bin"),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_Updates_Files_Increments_FilesUpdated_Creates_Parent_Dir()
    {
        var fs = new MockFileSystem();
        var srcRoot = "src";
        var dstRoot = "dst";
        fs.AddDirectory(srcRoot);
        fs.AddDirectory(dstRoot);

        fs.AddFile(J(fs, srcRoot, "x", "y", "z.txt"), new MockFileData("Z"));
        var t = new DateTime(2024, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        fs.File.SetLastWriteTimeUtc(J(fs, srcRoot, "x", "y", "z.txt"), t);

        fs.AddFile(J(fs, dstRoot, "x", "y", "z.txt"), new MockFileData(string.Empty));

        var copier = new Mock<IFileCopier>(MockBehavior.Loose);
        copier.Setup(c => c.AtomicCopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var engine = CreateEngine(fs, copier.Object);

        var stats = await engine.Run(
            srcRoot,
            dstRoot,
            Array.Empty<string>(),
            new[] { J(fs, "x", "y", "z.txt") },
            Array.Empty<string>(),
            Stats(),
            CancellationToken.None);

        Assert.Equal(0, stats.FilesCopied);
        Assert.Equal(1, stats.FilesUpdated);
        Assert.True(fs.Directory.Exists(J(fs, dstRoot, "x", "y")));
        Assert.Equal(t, fs.File.GetLastWriteTimeUtc(J(fs, dstRoot, "x", "y", "z.txt")));

        copier.Verify(
            c => c.AtomicCopyAsync(J(fs, srcRoot, "x", "y", "z.txt"), J(fs, dstRoot, "x", "y", "z.txt"),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_Mixed_Buckets_All_Handled()
    {
        var fs = new MockFileSystem();
        var srcRoot = "src";
        var dstRoot = "dst";
        fs.AddDirectory(srcRoot);
        fs.AddDirectory(dstRoot);

        fs.AddFile(J(fs, srcRoot, "copy.txt"), new MockFileData("1"));
        fs.AddFile(J(fs, srcRoot, "upd.txt"), new MockFileData("2"));
        fs.File.SetLastWriteTimeUtc(J(fs, srcRoot, "copy.txt"), new DateTime(2024, 1, 1, 1, 0, 0, DateTimeKind.Utc));
        fs.File.SetLastWriteTimeUtc(J(fs, srcRoot, "upd.txt"), new DateTime(2024, 1, 1, 2, 0, 0, DateTimeKind.Utc));

        fs.AddFile(J(fs, dstRoot, "copy.txt"), new MockFileData(string.Empty));
        fs.AddFile(J(fs, dstRoot, "upd.txt"), new MockFileData(string.Empty));

        var copier = new Mock<IFileCopier>(MockBehavior.Loose);
        copier.Setup(c => c.AtomicCopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var engine = CreateEngine(fs, copier.Object);

        var stats = await engine.Run(
            srcRoot,
            dstRoot,
            new[] { "copy.txt" },
            new[] { "upd.txt" },
            new[] { "dir1", "dir2/sub" },
            Stats(),
            CancellationToken.None);

        Assert.True(fs.Directory.Exists(J(fs, dstRoot, "dir1")));
        Assert.True(fs.Directory.Exists(J(fs, dstRoot, "dir2", "sub")));
        Assert.Equal(2, stats.DirsCreated);

        Assert.Equal(1, stats.FilesCopied);
        Assert.Equal(1, stats.FilesUpdated);

        copier.Verify(
            c => c.AtomicCopyAsync(J(fs, srcRoot, "copy.txt"), J(fs, dstRoot, "copy.txt"),
                It.IsAny<CancellationToken>()), Times.Once);
        copier.Verify(
            c => c.AtomicCopyAsync(J(fs, srcRoot, "upd.txt"), J(fs, dstRoot, "upd.txt"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}