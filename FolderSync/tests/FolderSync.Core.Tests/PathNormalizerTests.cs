using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using FolderSync.App.Cli;
using Moq;

namespace FolderSync.Core.Tests;

public class PathNormalizerTests
{
    private static string Trim(string s, MockFileSystem fs)
    {
        return s.TrimEnd(fs.Path.DirectorySeparatorChar, fs.Path.AltDirectorySeparatorChar);
    }

    [Fact]
    public void NormalizeExistingDirectory_Returns_Trimmed_FullPath_When_Exists_And_MustExist()
    {
        var fs = new MockFileSystem(new Dictionary<string, MockFileData>());
        fs.AddDirectory("/root/dir");
        var sut = new PathNormalizer(fs);

        var result = sut.NormalizeExistingDirectory("/root/dir///", true, "--source");

        var expected = Trim(fs.Path.GetFullPath("/root/dir///"), fs);
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizeExistingDirectory_Throws_When_NotExists_And_MustExist()
    {
        var fs = new MockFileSystem();
        var sut = new PathNormalizer(fs);

        Action act = () => sut.NormalizeExistingDirectory("/no/such/dir", true, "--source");

        act.Should().Throw<ArgumentException>().WithMessage("*--source*doesn't exist*");
    }

    [Fact]
    public void NormalizeExistingDirectory_Returns_Trimmed_FullPath_When_NotRequiredToExist()
    {
        var fs = new MockFileSystem();
        var sut = new PathNormalizer(fs);

        var result = sut.NormalizeExistingDirectory("/will/be/created/", false, "--replica");

        var expected = Trim(fs.Path.GetFullPath("/will/be/created/"), fs);
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizeDirectory_Returns_Trimmed_FullPath()
    {
        var fs = new MockFileSystem();
        var sut = new PathNormalizer(fs);

        var result = sut.NormalizeDirectory("/a/b/c///", "--replica");

        var expected = Trim(fs.Path.GetFullPath("/a/b/c///"), fs);
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizeFilePath_Returns_FullPath_And_Creates_Parent_Dir_If_Missing()
    {
        var fs = new MockFileSystem();
        var sut = new PathNormalizer(fs);

        var full = sut.NormalizeFilePath("/logs/app/app.log", "--log");

        full.Should().Be(fs.Path.GetFullPath("/logs/app/app.log"));
        fs.Directory.Exists("/logs/app").Should().BeTrue();
    }

    [Fact]
    public void NormalizeFilePath_Allows_Plain_FileName_Using_CurrentDirectory()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/wd");
        fs.Directory.SetCurrentDirectory("/wd");
        var sut = new PathNormalizer(fs);

        var full = sut.NormalizeFilePath("app.log", "--log");

        full.Should().Be(fs.Path.GetFullPath("/wd/app.log"));
        fs.Directory.Exists("/wd").Should().BeTrue();
    }

    [Fact]
    public void NormalizeFilePath_Does_Not_Create_Parent_If_Already_Exists()
    {
        var fs = new MockFileSystem();
        fs.AddDirectory("/var/log");
        var sut = new PathNormalizer(fs);

        var full = sut.NormalizeFilePath("/var/log/app.log", "--log");

        full.Should().Be(fs.Path.GetFullPath("/var/log/app.log"));
        fs.Directory.Exists("/var/log").Should().BeTrue();
    }

    [Fact]
    public void NormalizeFilePath_Wraps_Exception_From_GetFullPath()
    {
        var path = new Mock<PathBase>();
        path.Setup(p => p.GetFullPath(It.IsAny<string>()))
            .Throws(new InvalidOperationException("bad"));

        var dir = new Mock<DirectoryBase>();

        var fs = new Mock<IFileSystem>();
        fs.SetupGet(f => f.Path).Returns(path.Object);
        fs.SetupGet(f => f.Directory).Returns(dir.Object);

        var sut = new PathNormalizer(fs.Object);

        Action act = () => sut.NormalizeFilePath("::bad::", "--log");
        Action act1 = () => sut.NormalizeDirectory("::bad::", "--log");

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Incorrect path for --log*");

        act1.Should()
            .Throw<ArgumentException>()
            .WithMessage("Incorrect path for --log*");
    }
}