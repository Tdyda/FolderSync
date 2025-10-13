using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FolderSync.Core.Results;
using FolderSync.Core.Sync.Operations;
using FolderSync.Core.Sync.Scanning;
using FolderSync.Core.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FolderSync.Core.Tests;

public class DeletionEngineTests
{
    private static DeletionEngine Create(MockFileSystem fs)
    {
        return new DeletionEngine(new NullLogger<DeletionEngine>(), fs);
    }

    private static string J(MockFileSystem fs, params string[] parts)
    {
        var p = parts[0];
        for (var i = 1; i < parts.Length; i++)
            p = fs.Path.Combine(p, parts[i]);
        return p;
    }

    private static DirectorySnapshot Snap(MockFileSystem fs, string root,
        IEnumerable<string> dirs, IDictionary<string, FileMetadata> files)
    {
        return new DirectorySnapshot
        {
            RootPath = root,
            Directories = new HashSet<string>(dirs, PathComparer.ForPaths),
            Files = new Dictionary<string, FileMetadata>(files, PathComparer.ForPaths)
        };
    }

    private static DiffResult Diff(IEnumerable<string> dirsToDelete, IEnumerable<string> filesToDelete)
    {
        return new DiffResult
        {
            DirsToCreate = new HashSet<string>(PathComparer.ForPaths),
            DirsToDelete = new HashSet<string>(dirsToDelete, PathComparer.ForPaths),
            FilesToCopy = new HashSet<string>(PathComparer.ForPaths),
            FilesToUpdate = new HashSet<string>(PathComparer.ForPaths),
            FilesToDelete = new HashSet<string>(filesToDelete, PathComparer.ForPaths)
        };
    }

    [Fact]
    public async Task RemoveFiles_Deletes_Existing_Files_Including_ReadOnly()
    {
        var fs = new MockFileSystem();
        var root = "replica";
        fs.AddDirectory(root);

        fs.AddFile(J(fs, root, "a.txt"), new MockFileData("A"));
        fs.AddFile(J(fs, root, "ro.bin"),
            new MockFileData(new byte[] { 1, 2 }) { Attributes = FileAttributes.ReadOnly });

        var replica = Snap(fs, root, new[] { "" }, new Dictionary<string, FileMetadata>());
        var diff = Diff(Array.Empty<string>(), new[] { "a.txt", "ro.bin", "missing.dat" });

        var engine = Create(fs);
        var stats = await engine.Run(replica, diff, new OperationStats(), CancellationToken.None);

        Assert.False(fs.FileExists(J(fs, root, "a.txt")));
        Assert.False(fs.FileExists(J(fs, root, "ro.bin")));
        Assert.Equal(2, stats.FilesDeleted);
    }

    [Fact]
    public async Task RemoveDirectories_Deletes_Empty_Subdir_And_Skips_NonEmpty_Parent()
    {
        var fs = new MockFileSystem();
        var root = "replica";
        fs.AddDirectory(root);

        fs.AddDirectory(fs.Path.Combine(root, "dir"));
        fs.AddDirectory(fs.Path.Combine(root, "dir", "sub"));
        fs.AddDirectory(fs.Path.Combine(root, "nonempty"));
        fs.AddFile(fs.Path.Combine(root, "nonempty", "keep.txt"), new MockFileData("x"));

        var replica = new DirectorySnapshot
        {
            RootPath = root,
            Directories = new HashSet<string> { "", "dir", "dir/sub", "nonempty" },
            Files = new Dictionary<string, FileMetadata>()
        };

        var diff = new DiffResult
        {
            DirsToCreate = new HashSet<string>(),
            DirsToDelete = new HashSet<string> { "dir", "dir/sub", "nonempty" },
            FilesToCopy = new HashSet<string>(),
            FilesToUpdate = new HashSet<string>(),
            FilesToDelete = new HashSet<string>()
        };

        var engine = new DeletionEngine(new NullLogger<DeletionEngine>(), fs);
        var stats = await engine.Run(replica, diff, new OperationStats(), CancellationToken.None);

        Assert.False(fs.Directory.Exists(fs.Path.Combine(root, "dir", "sub")));
        Assert.True(fs.Directory.Exists(fs.Path.Combine(root, "dir")));
        Assert.True(fs.Directory.Exists(fs.Path.Combine(root, "nonempty")));
        Assert.Equal(1, stats.DirsDeleted);
    }


    [Fact]
    public async Task RemoveFiles_BenignException_IsCaught_NoIncrement_NoThrow()
    {
        var root = "replica";
        var fileRel = "bad.txt";
        var fileFull = $"{root}/{fileRel}";

        var path = new Mock<IPath>();
        path.SetupGet(p => p.DirectorySeparatorChar).Returns('/');
        path.SetupGet(p => p.AltDirectorySeparatorChar).Returns('\\');
        path.Setup(p => p.Combine(root, fileRel)).Returns(fileFull);

        var file = new Mock<IFile>();
        file.Setup(f => f.Exists(fileFull)).Returns(true);
        file.Setup(f => f.GetAttributes(fileFull)).Returns(FileAttributes.Normal);
        file.Setup(f => f.Delete(fileFull)).Throws(new UnauthorizedAccessException());

        var dir = new Mock<IDirectory>();
        dir.Setup(d => d.Exists(root)).Returns(true);

        var fsMock = new Mock<IFileSystem>();
        fsMock.SetupGet(f => f.Path).Returns(path.Object);
        fsMock.SetupGet(f => f.File).Returns(file.Object);
        fsMock.SetupGet(f => f.Directory).Returns(dir.Object);

        var engine = new DeletionEngine(new NullLogger<DeletionEngine>(), fsMock.Object);

        var replica = new DirectorySnapshot
            { RootPath = root, Directories = new HashSet<string>(), Files = new Dictionary<string, FileMetadata>() };
        var diff = Diff(Array.Empty<string>(), new[] { fileRel });

        var stats = await engine.Run(replica, diff, new OperationStats(), CancellationToken.None);

        Assert.Equal(0, stats.FilesDeleted);
    }

    [Fact]
    public async Task RemoveDirectories_BenignException_IsCaught_NoIncrement_NoThrow()
    {
        var root = "replica";
        var dirRel = "to-del";
        var dirFull = $"{root}/{dirRel}";

        var path = new Mock<IPath>();
        path.SetupGet(p => p.DirectorySeparatorChar).Returns('/');
        path.SetupGet(p => p.AltDirectorySeparatorChar).Returns('\\');
        path.Setup(p => p.Combine(root, dirRel)).Returns(dirFull);

        var dir = new Mock<IDirectory>();
        dir.Setup(d => d.Exists(root)).Returns(true);
        dir.Setup(d => d.Exists(dirFull)).Returns(true);
        dir.Setup(d => d.EnumerateFileSystemEntries(dirFull)).Returns(Array.Empty<string>());
        dir.Setup(d => d.Delete(dirFull)).Throws(new UnauthorizedAccessException());

        var fsMock = new Mock<IFileSystem>();
        fsMock.SetupGet(f => f.Path).Returns(path.Object);
        fsMock.SetupGet(f => f.Directory).Returns(dir.Object);

        var engine = new DeletionEngine(new NullLogger<DeletionEngine>(), fsMock.Object);

        var replica = new DirectorySnapshot
            { RootPath = root, Directories = new HashSet<string>(), Files = new Dictionary<string, FileMetadata>() };
        var diff = Diff(new[] { dirRel }, Array.Empty<string>());

        var stats = await engine.Run(replica, diff, new OperationStats(), CancellationToken.None);

        Assert.Equal(0, stats.DirsDeleted);
    }

    [Fact]
    public async Task Cancellation_Stops_Processing_Without_Deletions()
    {
        var fs = new MockFileSystem();
        var root = "replica";
        fs.AddDirectory(root);
        fs.AddFile(fs.Path.Combine(root, "a.txt"), new MockFileData("A"));
        fs.AddFile(fs.Path.Combine(root, "b.txt"), new MockFileData("B"));

        var replica = new DirectorySnapshot
        {
            RootPath = root,
            Directories = new HashSet<string>(),
            Files = new Dictionary<string, FileMetadata>()
        };

        var diff = new DiffResult
        {
            DirsToCreate = new HashSet<string>(),
            DirsToDelete = new HashSet<string>(),
            FilesToCopy = new HashSet<string>(),
            FilesToUpdate = new HashSet<string>(),
            FilesToDelete = new HashSet<string> { "a.txt", "b.txt" }
        };

        var engine = new DeletionEngine(new NullLogger<DeletionEngine>(), fs);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var stats = await engine.Run(replica, diff, new OperationStats(), cts.Token);

        Assert.True(fs.FileExists(fs.Path.Combine(root, "a.txt")));
        Assert.True(fs.FileExists(fs.Path.Combine(root, "b.txt")));
        Assert.Equal(0, stats.FilesDeleted);
        Assert.Equal(0, stats.DirsDeleted);
    }


    [Fact]
    public async Task RemoveDirectories_Skips_When_NotEmpty()
    {
        var fs = new MockFileSystem();
        var root = "replica";
        fs.AddDirectory(root);
        fs.AddDirectory(J(fs, root, "keep"));
        fs.AddDirectory(J(fs, root, "keep", "child"));
        fs.AddFile(J(fs, root, "keep", "child", "x.txt"), new MockFileData("x"));

        var replica = Snap(fs, root, new[] { "", "keep", "keep/child" }, new Dictionary<string, FileMetadata>());
        var diff = Diff(new[] { "keep" }, Array.Empty<string>());

        var engine = Create(fs);
        var stats = await engine.Run(replica, diff, new OperationStats(), CancellationToken.None);

        Assert.True(fs.Directory.Exists(J(fs, root, "keep")));
        Assert.Equal(0, stats.DirsDeleted);
    }
}