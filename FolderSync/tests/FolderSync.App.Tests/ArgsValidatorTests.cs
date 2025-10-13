using System.Globalization;
using FluentAssertions;
using FolderSync.App.Cli;
using FolderSync.App.Interfaces;
using FolderSync.Core.Configuration;
using Moq;

namespace FolderSync.App.Tests;

public class ArgsValidatorTests
{
    private static ParsedArgs MakeArgs(
        string? source = "/src",
        string? replica = "/dst",
        string? interval = "120",
        string? log = "/log/app.log",
        bool debug = false)
    {
        var tokens = new Dictionary<string, string>();
        if (source is not null) tokens["--source"] = source;
        if (replica is not null) tokens["--replica"] = replica;
        if (interval is not null) tokens["--interval"] = interval;
        if (log is not null) tokens["--log"] = log;

        var switches = new HashSet<string>(StringComparer.Ordinal);
        if (debug) switches.Add("--debug");

        return new ParsedArgs(tokens, switches);
    }

    private static ArgsValidator CreateSut(Mock<IPathNormalizer> normMock)
    {
        return new ArgsValidator(normMock.Object);
    }

    [Fact]
    public void ValidateArgs_Returns_SyncOptions_With_Normalized_Values_And_Seconds_Interval_And_Debug()
    {
        var norm = new Mock<IPathNormalizer>(MockBehavior.Strict);
        norm.Setup(n => n.NormalizeExistingDirectory("/src", true, "--source")).Returns("/SRC");
        norm.Setup(n => n.NormalizeDirectory("/dst", "--replica")).Returns("/DST");
        norm.Setup(n => n.NormalizeFilePath("/log/app.log", "--log")).Returns("/LOG/app.log");

        var sut = CreateSut(norm);

        var args = MakeArgs("/src", "/dst", "90", "/log/app.log", true);
        var result = sut.ValidateArgs(args);

        result.SourcePath.Should().Be("/SRC");
        result.ReplicaPath.Should().Be("/DST");
        result.LogFilePath.Should().Be("/LOG/app.log");
        result.Interval.Should().Be(TimeSpan.FromSeconds(90));
        result.IsDebug.Should().BeTrue();

        norm.Verify(n => n.NormalizeExistingDirectory("/src", true, "--source"), Times.Once);
        norm.Verify(n => n.NormalizeDirectory("/dst", "--replica"), Times.Once);
        norm.Verify(n => n.NormalizeFilePath("/log/app.log", "--log"), Times.Once);
        norm.VerifyNoOtherCalls();
    }

    [Fact]
    public void ValidateArgs_Parses_HHMMSS_Interval()
    {
        var norm = new Mock<IPathNormalizer>(MockBehavior.Loose);
        norm.Setup(n => n.NormalizeExistingDirectory(It.IsAny<string>(), true, "--source")).Returns("/SRC");
        norm.Setup(n => n.NormalizeDirectory(It.IsAny<string>(), "--replica")).Returns("/DST");
        norm.Setup(n => n.NormalizeFilePath(It.IsAny<string>(), "--log")).Returns("/LOG/app.log");

        var sut = CreateSut(norm);

        var args = MakeArgs(interval: "01:02:03");
        var result = sut.ValidateArgs(args);

        result.Interval.Should().Be(TimeSpan.Parse("01:02:03", CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void ValidateArgs_Throws_When_Interval_Is_Not_Positive(string raw)
    {
        var norm = new Mock<IPathNormalizer>(MockBehavior.Loose);
        var sut = new ArgsValidator(norm.Object);

        var args = MakeArgs(interval: raw);

        Action act = () => sut.ValidateArgs(args);

        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("*must be positive*");
    }

    [Fact]
    public void ValidateArgs_Throws_When_Interval_Has_Invalid_Format()
    {
        var norm = new Mock<IPathNormalizer>(MockBehavior.Loose);
        var sut = CreateSut(norm);

        var args = MakeArgs(interval: "not-a-time");

        Action act = () => sut.ValidateArgs(args);
        act.Should().Throw<ArgumentException>().WithMessage("*Incorrect format --interval*");
    }

    [Fact]
    public void ValidateArgs_Throws_When_Source_Missing()
    {
        var norm = new Mock<IPathNormalizer>(MockBehavior.Loose);
        var sut = CreateSut(norm);

        var args = MakeArgs(null);

        Action act = () => sut.ValidateArgs(args);
        act.Should().Throw<ArgumentException>().WithMessage("*--source*");
    }

    [Fact]
    public void ValidateArgs_Throws_When_Replica_Missing()
    {
        var norm = new Mock<IPathNormalizer>(MockBehavior.Loose);
        var sut = CreateSut(norm);

        var args = MakeArgs(replica: null);

        Action act = () => sut.ValidateArgs(args);
        act.Should().Throw<ArgumentException>().WithMessage("*--replica*");
    }

    [Fact]
    public void ValidateArgs_Throws_When_Log_Missing()
    {
        var norm = new Mock<IPathNormalizer>(MockBehavior.Loose);
        var sut = CreateSut(norm);

        var args = MakeArgs(log: null);

        Action act = () => sut.ValidateArgs(args);
        act.Should().Throw<ArgumentException>().WithMessage("*--log*");
    }

    [Fact]
    public void ValidateArgs_Trims_Token_Values()
    {
        var norm = new Mock<IPathNormalizer>(MockBehavior.Strict);
        norm.Setup(n => n.NormalizeExistingDirectory("/SRC", true, "--source")).Returns("/SRC");
        norm.Setup(n => n.NormalizeDirectory("/DST", "--replica")).Returns("/DST");
        norm.Setup(n => n.NormalizeFilePath("/LOG", "--log")).Returns("/LOG");

        var sut = CreateSut(norm);

        var args = MakeArgs("  /SRC  ", "  /DST ", "  15  ", "  /LOG  ");
        var result = sut.ValidateArgs(args);

        result.Interval.Should().Be(TimeSpan.FromSeconds(15));
    }
}