using FolderSync.App.Cli;

namespace FolderSync.App.Tests;

public class ArgsLexerTests
{
    [Fact]
    public void ToParsedArgs_Throws_When_NoArgs()
    {
        var lexer = new ArgsLexer();
        var ex = Assert.Throws<ArgumentException>(() => lexer.ToParsedArgs(Array.Empty<string>()));
        Assert.Contains("Missing require arguments", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToParsedArgs_Parses_Flags_And_Switches()
    {
        var args = new[]
        {
            "--source", "/src",
            "--replica", "/dst",
            "--interval", "15",
            "--log", "info",
            "--debug"
        };

        var lexer = new ArgsLexer();
        var parsed = lexer.ToParsedArgs(args);

        Assert.Equal("/src", parsed.Tokens["--source"]);
        Assert.Equal("/dst", parsed.Tokens["--replica"]);
        Assert.Equal("15", parsed.Tokens["--interval"]);
        Assert.Equal("info", parsed.Tokens["--log"]);

        Assert.Contains("--debug", parsed.Switches);
        Assert.Equal(4, parsed.Tokens.Count);
        Assert.Single(parsed.Switches);
    }

    [Fact]
    public void ToParsedArgs_Throws_On_Unknown_Token()
    {
        var args = new[] { "--source", "/src", "unexpected" };
        var lexer = new ArgsLexer();

        var ex = Assert.Throws<ArgumentException>(() => lexer.ToParsedArgs(args));
        Assert.Contains("Unexpected token", ex.Message);
    }

    [Fact]
    public void ToParsedArgs_Throws_When_Missing_Value_For_Flag()
    {
        var args = new[] { "--path" };
        var lexer = new ArgsLexer();

        var ex = Assert.Throws<ArgumentException>(() => lexer.ToParsedArgs(args));
        Assert.Contains("Expected flag --key value", ex.Message);
    }

    [Fact]
    public void ToParsedArgs_Ignores_Switches_When_Collecting_Flag_Values()
    {
        var args = new[] { "--debug", "--source", "/src", "--debug", "--replica", "/dst" };

        var lexer = new ArgsLexer();
        var parsed = lexer.ToParsedArgs(args);

        Assert.Equal("/src", parsed.Tokens["--source"]);
        Assert.Equal("/dst", parsed.Tokens["--replica"]);
        Assert.Contains("--debug", parsed.Switches);
        Assert.Equal(2, parsed.Tokens.Count);
        Assert.Single(parsed.Switches);
    }

    [Fact]
    public void ToParsedArgs_KnownFlagsAndSwitches_Are_CaseInsensitive_Keys_Preserved()
    {
        var args = new[] { "--SOURCE", "/s", "--RePlIcA", "/r", "--DeBuG" };

        var lexer = new ArgsLexer();
        var parsed = lexer.ToParsedArgs(args);

        Assert.Equal("/s", parsed.Tokens["--SOURCE"]);
        Assert.Equal("/r", parsed.Tokens["--RePlIcA"]);
        Assert.Contains("--DeBuG", parsed.Switches);

        Assert.Contains("--SOURCE", parsed.Tokens.Keys);
        Assert.Contains("--RePlIcA", parsed.Tokens.Keys);
    }
}