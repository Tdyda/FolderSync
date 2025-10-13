using FolderSync.Core.Sync.Diff;
using FolderSync.Core.Sync.Scanning;
using FolderSync.Core.Utilities;
using Microsoft.Extensions.Logging.Abstractions;

namespace FolderSync.Core.Tests;

public class DiffEngineTests
{
    private static DiffEngine CreateEngine()
    {
        return new DiffEngine(new NullLogger<DiffEngine>());
    }

    private static DirectorySnapshot Snapshot(
        IEnumerable<string> dirs,
        IDictionary<string, FileMetadata> files)
    {
        return new DirectorySnapshot
        {
            RootPath = "root",
            Directories = new HashSet<string>(dirs, PathComparer.ForPaths),
            Files = new Dictionary<string, FileMetadata>(files, PathComparer.ForPaths)
        };
    }

    private static FileMetadata Fm(long size, DateTime utc)
    {
        return new FileMetadata(size, utc);
    }

    [Fact]
    public void Compute_WhenSnapshotsEqual_ReturnsEmptyDiff()
    {
        var t = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var source = Snapshot(
            new[] { "", "a", "b/c" },
            new Dictionary<string, FileMetadata>
            {
                ["a/x.txt"] = Fm(10, t),
                ["b/c/y.bin"] = Fm(20, t)
            });

        var replica = Snapshot(
            new[] { "", "a", "b/c" },
            new Dictionary<string, FileMetadata>
            {
                ["a/x.txt"] = Fm(10, t),
                ["b/c/y.bin"] = Fm(20, t)
            });

        var engine = CreateEngine();
        var diff = engine.Compute(source, replica);

        Assert.Empty(diff.DirsToCreate);
        Assert.Empty(diff.DirsToDelete);
        Assert.Empty(diff.FilesToCopy);
        Assert.Empty(diff.FilesToDelete);
        Assert.Empty(diff.FilesToUpdate);
    }

    [Fact]
    public void Compute_Dirs_Create_And_Delete()
    {
        var source = Snapshot(
            new[] { "", "a", "b/c" },
            new Dictionary<string, FileMetadata>());

        var replica = Snapshot(
            new[] { "", "a", "z" },
            new Dictionary<string, FileMetadata>());

        var engine = CreateEngine();
        var diff = engine.Compute(source, replica);

        Assert.Contains("b/c", diff.DirsToCreate);
        Assert.DoesNotContain("", diff.DirsToCreate);
        Assert.Single(diff.DirsToCreate);

        Assert.Contains("z", diff.DirsToDelete);
        Assert.DoesNotContain("", diff.DirsToDelete);
        Assert.Single(diff.DirsToDelete);
    }

    [Fact]
    public void Compute_Files_Copy_And_Delete()
    {
        var t = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var source = Snapshot(
            new[] { "" },
            new Dictionary<string, FileMetadata>
            {
                ["only-in-source.txt"] = Fm(1, t),
                ["shared.txt"] = Fm(5, t)
            });

        var replica = Snapshot(
            new[] { "" },
            new Dictionary<string, FileMetadata>
            {
                ["only-in-replica.txt"] = Fm(2, t),
                ["shared.txt"] = Fm(5, t)
            });

        var engine = CreateEngine();
        var diff = engine.Compute(source, replica);

        Assert.Contains("only-in-source.txt", diff.FilesToCopy);
        Assert.DoesNotContain("shared.txt", diff.FilesToCopy);
        Assert.Single(diff.FilesToCopy);

        Assert.Contains("only-in-replica.txt", diff.FilesToDelete);
        Assert.DoesNotContain("shared.txt", diff.FilesToDelete);
        Assert.Single(diff.FilesToDelete);

        Assert.Empty(diff.FilesToUpdate);
    }

    [Fact]
    public void Compute_FilesToUpdate_When_SizeDiffers()
    {
        var t = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var source = Snapshot(
            new[] { "" },
            new Dictionary<string, FileMetadata>
            {
                ["file.dat"] = Fm(100, t)
            });

        var replica = Snapshot(
            new[] { "" },
            new Dictionary<string, FileMetadata>
            {
                ["file.dat"] = Fm(200, t)
            });

        var engine = CreateEngine();
        var diff = engine.Compute(source, replica);

        Assert.Contains("file.dat", diff.FilesToUpdate);
        Assert.Single(diff.FilesToUpdate);
    }

    [Fact]
    public void Compute_FilesToUpdate_When_TimeDiff_GreaterThan2s_SameSize()
    {
        var t1 = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddSeconds(3);

        var source = Snapshot(
            new[] { "" },
            new Dictionary<string, FileMetadata>
            {
                ["file.dat"] = Fm(100, t1)
            });

        var replica = Snapshot(
            new[] { "" },
            new Dictionary<string, FileMetadata>
            {
                ["file.dat"] = Fm(100, t2)
            });

        var engine = CreateEngine();
        var diff = engine.Compute(source, replica);

        Assert.Contains("file.dat", diff.FilesToUpdate);
        Assert.Single(diff.FilesToUpdate);
    }

    [Fact]
    public void Compute_NoUpdate_When_TimeDiff_Within2s_SameSize()
    {
        var t1 = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddSeconds(2);

        var source = Snapshot(
            new[] { "" },
            new Dictionary<string, FileMetadata>
            {
                ["file.dat"] = Fm(100, t1)
            });

        var replica = Snapshot(
            new[] { "" },
            new Dictionary<string, FileMetadata>
            {
                ["file.dat"] = Fm(100, t2)
            });

        var engine = CreateEngine();
        var diff = engine.Compute(source, replica);

        Assert.Empty(diff.FilesToUpdate);
    }

    [Fact]
    public void Compute_Mixed_AllBuckets_Filled()
    {
        var t = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var source = Snapshot(
            new[] { "", "newdir", "kept" },
            new Dictionary<string, FileMetadata>
            {
                ["copy.txt"] = Fm(1, t),
                ["update-size.bin"] = Fm(10, t),
                ["update-time.bin"] = Fm(20, t)
            });

        var replica = Snapshot(
            new[] { "", "todelete", "kept" },
            new Dictionary<string, FileMetadata>
            {
                ["delete.txt"] = Fm(2, t),
                ["update-size.bin"] = Fm(99, t),
                ["update-time.bin"] = Fm(20, t.AddSeconds(5))
            });

        var engine = CreateEngine();
        var diff = engine.Compute(source, replica);

        Assert.Contains("newdir", diff.DirsToCreate);
        Assert.Contains("todelete", diff.DirsToDelete);
        Assert.Contains("copy.txt", diff.FilesToCopy);
        Assert.Contains("delete.txt", diff.FilesToDelete);
        Assert.Contains("update-size.bin", diff.FilesToUpdate);
        Assert.Contains("update-time.bin", diff.FilesToUpdate);
    }
}