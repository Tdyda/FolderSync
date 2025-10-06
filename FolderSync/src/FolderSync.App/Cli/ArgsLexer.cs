namespace FolderSync.App.Cli;

public class ArgsLexer
{
    private static readonly HashSet<string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        "--source",
        "--replica",
        "--interval",
        "--log",
        "--help",
        "-h",
        "/?"
    };

    private static readonly HashSet<string> SwitchFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "--debug"
    };



    public static Dictionary<string, string> ToDictionary(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Missing require arguments. Use --help");

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i += 2)
        {
            var token = args[i];
            if (token.StartsWith("-", StringComparison.Ordinal))
            {
                IsKnown(token);

                if(IsHelpRequested(token)) break;

                if (SwitchFlags.Contains(token))
                {
                    dict[token] = "true";
                    continue;
                }

                if (i + 1 >= args.Length || args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    throw new ArgumentException($"Flag {token} require value");

                dict[token] = args[i + 1];
            }
            else
                throw new ArgumentException($"Unexpected token: {token}. Expected flag --key value");
        }

        return dict;
    }

    private static void IsKnown(string token)
    {
        if (!Known.Contains(token) && !SwitchFlags.Contains(token))
            throw new ArgumentException($"Unknown flag: {token}");
    }

    public static bool IsHelpRequested(string[] args) =>
        args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(a, "/?", StringComparison.OrdinalIgnoreCase));
    private static bool IsHelpRequested(string token)
    {
        return token.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
               token.Equals("/?", StringComparison.OrdinalIgnoreCase);
    }
}