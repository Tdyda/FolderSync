using FluentAssertions;
using FolderSync.App.Cli;
using FolderSync.App.Interfaces;
using FolderSync.Core.Configuration;
using Moq;

namespace FolderSync.App.Tests;

public class AppRunnerTests
{
    private static (StringWriter swOut, StringWriter swErr, TextWriter origOut, TextWriter origErr) CaptureConsole()
    {
        var swOut = new StringWriter();
        var swErr = new StringWriter();
        var origOut = Console.Out;
        var origErr = Console.Error;
        Console.SetOut(swOut);
        Console.SetError(swErr);
        return (swOut, swErr, origOut, origErr);
    }

    private static void RestoreConsole(TextWriter origOut, TextWriter origErr)
    {
        Console.SetOut(origOut);
        Console.SetError(origErr);
    }

    private static string[] ValidArgs()
    {
        return new[] { "--source", "/src", "--replica", "/dst", "--interval", "1", "--log", "/tmp/sync.log" };
    }

    private static ParsedArgs ParsedFrom(string[] args)
    {
        return new ParsedArgs(
            new Dictionary<string, string>
            {
                ["--source"] = args[1],
                ["--replica"] = args[3],
                ["--interval"] = args[5],
                ["--log"] = args[7]
            },
            new HashSet<string>());
    }

    private static SyncOptions OptionsFrom(ParsedArgs p)
    {
        return new SyncOptions
        {
            SourcePath = p.Tokens["--source"],
            ReplicaPath = p.Tokens["--replica"],
            Interval = TimeSpan.FromSeconds(int.Parse(p.Tokens["--interval"])),
            LogFilePath = p.Tokens["--log"],
            IsDebug = false
        };
    }

    [Fact]
    public async Task HelpArg_PrintsUsage_ToOut_And_Returns_Success_And_DoesNotInvoke_Parser_Validator()
    {
        var (swOut, _, origOut, origErr) = CaptureConsole();
        try
        {
            var lexer = new Mock<IArgsLexer>(MockBehavior.Strict);
            var validator = new Mock<IArgsValidator>(MockBehavior.Strict);

            var runner = new AppRunner(
                new[] { "--help" },
                lexer.Object,
                _ => throw new Exception("factory should not be called"),
                validator.Object);

            var code = await runner.RunAsync();

            code.Should().Be((int)ExitCode.Success);
            swOut.ToString().Should().Contain("Usage:");
            lexer.VerifyNoOtherCalls();
            validator.VerifyNoOtherCalls();
        }
        finally
        {
            RestoreConsole(origOut, origErr);
        }
    }

    [Fact]
    public async Task
        NoArgs_PrintsErrorAndUsage_ToError_And_Returns_InvalidArguments_And_DoesNotInvoke_Parser_Validator()
    {
        var (_, swErr, origOut, origErr) = CaptureConsole();
        try
        {
            var lexer = new Mock<IArgsLexer>(MockBehavior.Strict);
            var validator = new Mock<IArgsValidator>(MockBehavior.Strict);

            var runner = new AppRunner(
                Array.Empty<string>(),
                lexer.Object,
                _ => throw new Exception("factory should not be called"),
                validator.Object);

            var code = await runner.RunAsync();

            code.Should().Be((int)ExitCode.InvalidArguments);
            var err = swErr.ToString();
            err.Should().Contain("Missing arguments.");
            err.Should().Contain("Usage:");
            lexer.VerifyNoOtherCalls();
            validator.VerifyNoOtherCalls();
        }
        finally
        {
            RestoreConsole(origOut, origErr);
        }
    }

    [Fact]
    public async Task HappyPath_Parses_Validates_CreatesLoop_And_Returns_Success_When_Factory_Throws_OperationCanceled()
    {
        var (_, swErr, origOut, origErr) = CaptureConsole();
        try
        {
            var args = ValidArgs();
            var parsed = ParsedFrom(args);
            var opts = OptionsFrom(parsed);

            var lexer = new Mock<IArgsLexer>(MockBehavior.Strict);
            lexer.Setup(l => l.ToParsedArgs(args)).Returns(parsed);

            var validator = new Mock<IArgsValidator>(MockBehavior.Strict);
            validator.Setup(v => v.ValidateArgs(parsed)).Returns(opts);

            var runner = new AppRunner(
                args,
                lexer.Object,
                _ => throw new OperationCanceledException(),
                validator.Object);

            var code = await runner.RunAsync();

            code.Should().Be((int)ExitCode.Success);
            swErr.ToString().Should().BeEmpty();
            lexer.VerifyAll();
            validator.VerifyAll();
        }
        finally
        {
            RestoreConsole(origOut, origErr);
        }
    }

    [Fact]
    public async Task ValidatorThrows_PrintsInvalidArgumentsAndUsage_ToError_And_Returns_InvalidArguments()
    {
        var (_, swErr, origOut, origErr) = CaptureConsole();
        try
        {
            var args = ValidArgs();
            var parsed = ParsedFrom(args);

            var lexer = new Mock<IArgsLexer>(MockBehavior.Strict);
            lexer.Setup(l => l.ToParsedArgs(args)).Returns(parsed);

            var validator = new Mock<IArgsValidator>(MockBehavior.Strict);
            validator.Setup(v => v.ValidateArgs(parsed)).Throws(new ArgumentException("boom"));

            var runner = new AppRunner(
                args,
                lexer.Object,
                _ => throw new Exception("factory should not be called"),
                validator.Object);

            var code = await runner.RunAsync();

            code.Should().Be((int)ExitCode.InvalidArguments);
            var err = swErr.ToString();
            err.Should().Contain("Invalid arguments: boom");
            err.Should().Contain("Usage:");
            lexer.VerifyAll();
            validator.VerifyAll();
        }
        finally
        {
            RestoreConsole(origOut, origErr);
        }
    }

    [Fact]
    public async Task FactoryThrowsUnexpected_PrintsError_ToError_And_Returns_UnhandledException()
    {
        var (_, swErr, origOut, origErr) = CaptureConsole();
        try
        {
            var args = ValidArgs();
            var parsed = ParsedFrom(args);
            var opts = OptionsFrom(parsed);

            var lexer = new Mock<IArgsLexer>(MockBehavior.Strict);
            lexer.Setup(l => l.ToParsedArgs(args)).Returns(parsed);

            var validator = new Mock<IArgsValidator>(MockBehavior.Strict);
            validator.Setup(v => v.ValidateArgs(parsed)).Returns(opts);

            var runner = new AppRunner(
                args,
                lexer.Object,
                _ => throw new InvalidOperationException("boom"),
                validator.Object);

            var code = await runner.RunAsync();

            code.Should().Be((int)ExitCode.UnhandledException);
            swErr.ToString().Should().Contain("Unexpected error:");
            lexer.VerifyAll();
            validator.VerifyAll();
        }
        finally
        {
            RestoreConsole(origOut, origErr);
        }
    }
}