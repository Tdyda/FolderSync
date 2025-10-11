using FolderSync.App.Cli.Interfaces;
using FolderSync.Core.Configuration;

namespace FolderSync.App.Cli;

public class ArgsLexer : IArgsLexer
{
    private readonly HashSet<string> _knownFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "--source",
        "--replica",
        "--interval",
        "--log"
    };

    private readonly HashSet<string> _knowSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--debug",
        "--help",
        "-h",
        "/?"
    };


    public ParsedArgs ToParsedArgs(string[] args)
    {
        var result = new ParsedArgs();

        if (args.Length == 0)
            throw new ArgumentException("Missing require arguments. Use --help");

        foreach (var token in args)
            if (token.StartsWith('-') && _knowSwitches.Contains(token))
                result.Switches.Add(token);

        string? pendingOption = null;
        foreach (var token in args)
        {
            if (token.StartsWith('-') && _knowSwitches.Contains(token)) continue;

            if (pendingOption != null)
            {
                result.Flags[pendingOption] = token;
                pendingOption = null;
                continue;
            }

            if (token.StartsWith('-') && _knownFlags.Contains(token))
            {
                pendingOption = token;
                continue;
            }

            throw new ArgumentException($"Unexpected token: {token}. Expected flag --key value");
        }

        return result;
    }

    public bool IsHelpRequested(string[] args)
    {
        return args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(a, "/?", StringComparison.OrdinalIgnoreCase));
    }
}