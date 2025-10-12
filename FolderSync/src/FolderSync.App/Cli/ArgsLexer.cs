using FolderSync.Core.Configuration;

namespace FolderSync.App.Cli;

public class ArgsLexer
{
    private readonly HashSet<string> _knownFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "--source",
        "--replica",
        "--interval",
        "--log"
    };

    private readonly HashSet<string> _knownSwitches = new(StringComparer.OrdinalIgnoreCase)
    {
        "--debug"
    };


    public ParsedArgs ToParsedArgs(string[] args)
    {
        var switches = new HashSet<string>(StringComparer.Ordinal);
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);

        if (args.Length == 0)
            throw new ArgumentException("Missing require arguments. Use --help");

        foreach (var token in args)
            if (token.StartsWith('-') && _knownSwitches.Contains(token))
                switches.Add(token);

        string? pendingOption = null;
        foreach (var token in args)
        {
            if (token.StartsWith('-') && _knownSwitches.Contains(token)) continue;

            if (pendingOption != null)
            {
                tokens[pendingOption] = token;
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

        return new ParsedArgs
        (
            tokens,
            switches
        );
    }
}