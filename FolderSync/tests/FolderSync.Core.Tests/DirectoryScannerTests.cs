using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FolderSync.Core.Sync.Scanning;
using FolderSync.Core.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FolderSync.Core.Tests;

public class DirectoryScannerTests
{
    private static DirectoryScanner CreateScanner(IFileSystem fs)
    {
        return new DirectoryScanner(new NullLogger<DirectoryScanner>(), fs);
    }

    private static string Join(MockFileSystem fs, params string[] parts)
    {
        var p = parts[0];
        for (var i = 1; i < parts.Length; i++)
            p = fs.Path.Combine(p, parts[i]);
        return p;
    }

    private static string Rel(MockFileSystem fs, params string[] parts)
    {
        return Join(fs, parts).TrimEnd(fs.Path.DirectorySeparatorChar);
    }

    private static string Root(MockFileSystem fs)
    {
        return "root";
    }

    [Fact]
    public async Task BuildSnapshotAsync_Throws_WhenRootDoesNotExist()
    {
        var fs = new MockFileSystem();
        var scanner = CreateScanner(fs);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => scanner.BuildSnapshotAsync(Root(fs)));
    }

    [Fact]
    public async Task BuildSnapshotAsync_CollectsDirectoriesAndFiles_RelativePathsOk()
    {
        var fs = new MockFileSystem();
        var root = Root(fs);
        fs.AddDirectory(root);
        fs.AddDirectory(Join(fs, root, "sub"));
        fs.AddDirectory(Join(fs, root, "sub", "child"));
        fs.AddFile(Join(fs, root, "fileA.txt"), new MockFileData("A"));
        fs.AddFile(Join(fs, root, "sub", "fileB.bin"), new MockFileData(new byte[] { 1, 2 }));
        fs.AddFile(Join(fs, root, "sub", "child", "fileC.txt"), new MockFileData("CCC"));
        var tA = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Utc);
        var tB = new DateTime(2024, 02, 03, 04, 05, 06, DateTimeKind.Utc);
        var tC = new DateTime(2024, 03, 04, 05, 06, 07, DateTimeKind.Utc);
        fs.File.SetLastWriteTimeUtc(Join(fs, root, "fileA.txt"), tA);
        fs.File.SetLastWriteTimeUtc(Join(fs, root, "sub", "fileB.bin"), tB);
        fs.File.SetLastWriteTimeUtc(Join(fs, root, "sub", "child", "fileC.txt"), tC);

        var scanner = CreateScanner(fs);
        var snapshot = await scanner.BuildSnapshotAsync(root, CancellationToken.None);

        Assert.Equal(root, snapshot.RootPath);

        var expectedDirs = new HashSet<string>(PathComparer.ForPaths)
        {
            string.Empty,
            Rel(fs, "sub"),
            Rel(fs, "sub", "child")
        };
        Assert.True(expectedDirs.SetEquals(snapshot.Directories));

        Assert.Equal(3, snapshot.Files.Count);

        Assert.True(snapshot.Files.TryGetValue(Rel(fs, "fileA.txt"), out var metaA));
        Assert.Equal(1, metaA.Size);
        Assert.Equal(tA, metaA.LastWriteTimeUtc);

        Assert.True(snapshot.Files.TryGetValue(Rel(fs, "sub", "fileB.bin"), out var metaB));
        Assert.Equal(2, metaB.Size);
        Assert.Equal(tB, metaB.LastWriteTimeUtc);

        Assert.True(snapshot.Files.TryGetValue(Rel(fs, "sub", "child", "fileC.txt"), out var metaC));
        Assert.Equal(3, metaC.Size);
        Assert.Equal(tC, metaC.LastWriteTimeUtc);
    }

    [Fact]
    public async Task BuildSnapshotAsync_Skips_ReparsePointFiles()
    {
        var fs = new MockFileSystem();
        var root = Root(fs);
        fs.AddDirectory(root);
        fs.AddFile(Join(fs, root, "normal.txt"), new MockFileData("data"));
        var link = new MockFileData("ignored") { Attributes = FileAttributes.ReparsePoint };
        fs.AddFile(Join(fs, root, "link.dat"), link);
        var tNormal = new DateTime(2024, 04, 01, 12, 0, 0, DateTimeKind.Utc);
        fs.File.SetLastWriteTimeUtc(Join(fs, root, "normal.txt"), tNormal);

        var scanner = CreateScanner(fs);
        var snapshot = await scanner.BuildSnapshotAsync(root);

        Assert.Single(snapshot.Files);
        Assert.True(snapshot.Files.TryGetValue(Rel(fs, "normal.txt"), out var meta));
        Assert.Equal(4, meta.Size);
        Assert.Equal(tNormal, meta.LastWriteTimeUtc);
        Assert.DoesNotContain(Rel(fs, "link.dat"), snapshot.Files.Keys);
    }

    [Fact]
    public async Task BuildSnapshotAsync_Handles_DeepTreeAndEmptyDirs()
    {
        var fs = new MockFileSystem();
        var root = Root(fs);
        fs.AddDirectory(root);
        fs.AddDirectory(Join(fs, root, "empty"));
        fs.AddDirectory(Join(fs, root, "a"));
        fs.AddDirectory(Join(fs, root, "a", "b"));
        fs.AddDirectory(Join(fs, root, "a", "b", "c"));
        fs.AddFile(Join(fs, root, "a", "b", "c", "foo.txt"), new MockFileData("xyz"));

        var scanner = CreateScanner(fs);
        var snapshot = await scanner.BuildSnapshotAsync(root);

        var expected = new HashSet<string>(PathComparer.ForPaths)
        {
            string.Empty,
            Rel(fs, "empty"),
            Rel(fs, "a"),
            Rel(fs, "a", "b"),
            Rel(fs, "a", "b", "c")
        };
        Assert.True(expected.SetEquals(snapshot.Directories));
        Assert.Contains(Rel(fs, "a", "b", "c", "foo.txt"), snapshot.Files.Keys);
    }

    [Fact]
    public async Task BuildSnapshotAsync_WhenEnumerateDirectoriesThrowsBenign_Continues()
    {
        var root = "root";

        var pathMock = new Mock<IPath>();
        pathMock.SetupGet(p => p.DirectorySeparatorChar).Returns('/');
        pathMock.SetupGet(p => p.AltDirectorySeparatorChar).Returns('\\');
        pathMock.Setup(p => p.GetRelativePath(root, root)).Returns(string.Empty);
        pathMock.Setup(p => p.GetRelativePath(root, It.Is<string>(s => s.StartsWith("root/"))))
            .Returns<string, string>((_, full) => full.Substring(root.Length + 1));

        var dirMock = new Mock<IDirectory>();
        dirMock.Setup(d => d.Exists(root)).Returns(true);
        dirMock.Setup(d => d.EnumerateDirectories(root)).Throws(new UnauthorizedAccessException());
        dirMock.Setup(d => d.EnumerateFiles(root)).Returns(Array.Empty<string>());

        var fsMock = new Mock<IFileSystem>();
        fsMock.SetupGet(f => f.Path).Returns(pathMock.Object);
        fsMock.SetupGet(f => f.Directory).Returns(dirMock.Object);
        fsMock.SetupGet(f => f.DirectoryInfo).Returns(new Mock<IDirectoryInfoFactory>().Object);
        fsMock.SetupGet(f => f.File).Returns(new Mock<IFile>().Object);
        fsMock.SetupGet(f => f.FileInfo).Returns(new Mock<IFileInfoFactory>().Object);

        var scanner = CreateScanner(fsMock.Object);
        var snapshot = await scanner.BuildSnapshotAsync(root);

        var expectedDirs = new HashSet<string>(PathComparer.ForPaths) { string.Empty };
        Assert.True(expectedDirs.SetEquals(snapshot.Directories));
        Assert.Empty(snapshot.Files);
    }

    [Fact]
    public async Task BuildSnapshotAsync_WhenEnumerateFilesThrows_WarnsAndContinues()
    {
        var root = "root";

        var pathMock = new Mock<IPath>();
        pathMock.SetupGet(p => p.DirectorySeparatorChar).Returns('/');
        pathMock.SetupGet(p => p.AltDirectorySeparatorChar).Returns('\\');
        pathMock.Setup(p => p.GetRelativePath(root, root)).Returns(string.Empty);

        var dirMock = new Mock<IDirectory>();
        dirMock.Setup(d => d.Exists(root)).Returns(true);
        dirMock.Setup(d => d.EnumerateDirectories(root)).Returns(Array.Empty<string>());
        dirMock.Setup(d => d.EnumerateFiles(root)).Throws(new UnauthorizedAccessException());

        var fsMock = new Mock<IFileSystem>();
        fsMock.SetupGet(f => f.Path).Returns(pathMock.Object);
        fsMock.SetupGet(f => f.Directory).Returns(dirMock.Object);
        fsMock.SetupGet(f => f.DirectoryInfo).Returns(new Mock<IDirectoryInfoFactory>().Object);
        fsMock.SetupGet(f => f.File).Returns(new Mock<IFile>().Object);
        fsMock.SetupGet(f => f.FileInfo).Returns(new Mock<IFileInfoFactory>().Object);

        var scanner = CreateScanner(fsMock.Object);
        var snapshot = await scanner.BuildSnapshotAsync(root);

        var expectedDirs = new HashSet<string>(PathComparer.ForPaths) { string.Empty };
        Assert.True(expectedDirs.SetEquals(snapshot.Directories));
        Assert.Empty(snapshot.Files);
    }

    [Fact]
    public async Task BuildSnapshotAsync_WhenReadingSingleFileMetadataThrowsBenign_SkipsFile()
    {
        var root = "root";
        var sep = '/';
        var filePath = $"root{sep}a.txt";

        var pathMock = new Mock<IPath>();
        pathMock.SetupGet(p => p.DirectorySeparatorChar).Returns(sep);
        pathMock.SetupGet(p => p.AltDirectorySeparatorChar).Returns('\\');
        pathMock.Setup(p => p.GetRelativePath(root, filePath)).Returns("a.txt");

        var dirMock = new Mock<IDirectory>();
        dirMock.Setup(d => d.Exists(root)).Returns(true);
        dirMock.Setup(d => d.EnumerateDirectories(root)).Returns(Array.Empty<string>());
        dirMock.Setup(d => d.EnumerateFiles(root)).Returns(new[] { filePath });

        var fi = new Mock<IFileInfo>();
        fi.SetupGet(f => f.Attributes).Returns(FileAttributes.Normal);
        fi.SetupGet(f => f.LinkTarget).Returns((string)null);
        fi.SetupGet(f => f.Length).Throws(new UnauthorizedAccessException());

        var fiFactory = new Mock<IFileInfoFactory>();
        fiFactory.Setup(f => f.New(filePath)).Returns(fi.Object);

        var fsMock = new Mock<IFileSystem>();
        fsMock.SetupGet(f => f.Path).Returns(pathMock.Object);
        fsMock.SetupGet(f => f.Directory).Returns(dirMock.Object);
        fsMock.SetupGet(f => f.DirectoryInfo).Returns(new Mock<IDirectoryInfoFactory>().Object);
        fsMock.SetupGet(f => f.File).Returns(new Mock<IFile>().Object);
        fsMock.SetupGet(f => f.FileInfo).Returns(fiFactory.Object);

        var scanner = CreateScanner(fsMock.Object);
        var snapshot = await scanner.BuildSnapshotAsync(root);

        var expectedDirs = new HashSet<string>(PathComparer.ForPaths) { string.Empty };
        Assert.True(expectedDirs.SetEquals(snapshot.Directories));
        Assert.Empty(snapshot.Files);
    }

    [Fact]
    public async Task Skips_Directory_When_Attributes_Have_ReparsePoint()
    {
        var root = "root";
        var sub = "root/sub";

        var path = new Mock<IPath>();
        path.SetupGet(p => p.DirectorySeparatorChar).Returns('/');
        path.SetupGet(p => p.AltDirectorySeparatorChar).Returns('\\');
        path.Setup(p => p.GetRelativePath(root, root)).Returns(string.Empty);
        path.Setup(p => p.GetRelativePath(root, sub)).Returns("sub");

        var di = new Mock<IDirectoryInfo>();
        di.SetupGet(d => d.Attributes).Returns(FileAttributes.ReparsePoint);
        di.SetupGet(d => d.LinkTarget).Returns((string?)null);

        var diFactory = new Mock<IDirectoryInfoFactory>();
        diFactory.Setup(f => f.New(sub)).Returns(di.Object);

        var dir = new Mock<IDirectory>();
        dir.Setup(d => d.Exists(root)).Returns(true);
        dir.Setup(d => d.EnumerateDirectories(root)).Returns(new[] { sub });
        dir.Setup(d => d.EnumerateFiles(root)).Returns(Array.Empty<string>());

        var fs = new Mock<IFileSystem>();
        fs.SetupGet(f => f.Path).Returns(path.Object);
        fs.SetupGet(f => f.Directory).Returns(dir.Object);
        fs.SetupGet(f => f.DirectoryInfo).Returns(diFactory.Object);

        var scanner = CreateScanner(fs.Object);
        var snapshot = await scanner.BuildSnapshotAsync(root);

        Assert.Single(snapshot.Directories);
        Assert.Contains(string.Empty, snapshot.Directories);
        Assert.Empty(snapshot.Files);
    }

    [Fact]
    public async Task Skips_Directory_When_LinkTarget_Is_Set()
    {
        var root = "root";
        var sub = "root/sub";

        var path = new Mock<IPath>();
        path.SetupGet(p => p.DirectorySeparatorChar).Returns('/');
        path.SetupGet(p => p.AltDirectorySeparatorChar).Returns('\\');
        path.Setup(p => p.GetRelativePath(root, root)).Returns(string.Empty);
        path.Setup(p => p.GetRelativePath(root, sub)).Returns("sub");

        var di = new Mock<IDirectoryInfo>();
        di.SetupGet(d => d.Attributes).Returns(FileAttributes.Normal);
        di.SetupGet(d => d.LinkTarget).Returns("/some/target");

        var diFactory = new Mock<IDirectoryInfoFactory>();
        diFactory.Setup(f => f.New(sub)).Returns(di.Object);

        var dir = new Mock<IDirectory>();
        dir.Setup(d => d.Exists(root)).Returns(true);
        dir.Setup(d => d.EnumerateDirectories(root)).Returns(new[] { sub });
        dir.Setup(d => d.EnumerateFiles(root)).Returns(Array.Empty<string>());

        var fs = new Mock<IFileSystem>();
        fs.SetupGet(f => f.Path).Returns(path.Object);
        fs.SetupGet(f => f.Directory).Returns(dir.Object);
        fs.SetupGet(f => f.DirectoryInfo).Returns(diFactory.Object);

        var scanner = CreateScanner(fs.Object);
        var snapshot = await scanner.BuildSnapshotAsync(root);

        Assert.Single(snapshot.Directories);
        Assert.Contains(string.Empty, snapshot.Directories);
        Assert.Empty(snapshot.Files);
    }
}