using System.Globalization;
using FolderSync.Core.Options;

namespace FolderSync.App.Cli;

public static class ArgsParser
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

    public static bool IsHelpRequested(string[] args) =>
        args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(a, "/?", StringComparison.OrdinalIgnoreCase));


    public static SyncOptions Parse(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Brak argumentów. Użyj --help");

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                if (!Known.Contains(token))
                    throw new ArgumentException($"Nieznana flaga: {token}");

                if (token.Equals("--help", StringComparison.OrdinalIgnoreCase)) continue;

                if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    throw new ArgumentException($"Flaga {token} wymaga wartości");

                dict[token] = args[++i];
            }
            else
                throw new ArgumentException($"Nieoczekiwany token: {token}. Oczekiwano flag --key value");
        }

        string source = Require(dict, "--source");
        string replica = Require(dict, "--replica");
        string intervalStr = Require(dict, "--interval");
        string logPath = Require(dict, "--log");

        string normSource = NormalizeExistingDirectory(source, mustExist: true, name: "--source");
        string normReplica = NormalizeDirectory(replica, name: "--replica");
        string normLog = NormalizeFilePath(logPath, name: "--log");

        var interval = ParseInterval(intervalStr);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("--interval musi być dodatni.");

        return new SyncOptions
        {
            SourcePath = normSource,
            ReplicaPath = normReplica,
            Interval = interval,
            LogFilePath = normLog,
        };
    }

    private static TimeSpan ParseInterval(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds))
        {
            if (seconds <= 0) throw new ArgumentException("--interval w sekundach musi być > 0");
            return TimeSpan.FromSeconds(seconds);
        }

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var ts))
            return ts;
        throw new ArgumentException("Nieprawidłowy format --interval. Użyj liczby sekund lub HH:MM:SS.");
    }

    private static string Require(Dictionary<string, string> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Brak wymaganej flagi {key}");
        return value.Trim();
    }

    private static string NormalizeExistingDirectory(string path, bool mustExist, string name)
    {
        string full = Path.GetFullPath(path);
        if (mustExist && !Directory.Exists(full))
            throw new ArgumentException($"Ścieżka {name} nie istnieje: {full}");
        return TrimEndingSeparators(full);
    }

    private static string NormalizeDirectory(string path, string name)
    {
        try
        {
            string full = Path.GetFullPath(path);
            return TrimEndingSeparators(full);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Nieprawidłowa ścieżka dla {name}: {path}. {ex.Message}");
        }
    }

    private static string NormalizeFilePath(string path, string name)
    {
        try
        {
            string full = Path.GetFullPath(path);
            var dir = Path.GetDirectoryName(full);
            if (string.IsNullOrWhiteSpace(dir))
                throw new ArgumentException($"{name}: ścieżka pliku musi zawierać katalog nadrzędny");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return full;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Nieprawidłowa ścieżka dla {name}: {path}. {ex.Message}");
        }
    }

    private static string TrimEndingSeparators(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return path;
        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static void PrintUsage(TextWriter output)
    {
        output.WriteLine(
            @"Usage:
            FolderSync --source <dir> --replica <dir> --interval <seconds|HH:MM:SS> --log <file>

            Examples:
            FolderSync --source C:\Data --replica D:\Mirror --interval 300 --log C:\logs\sync.log
            FolderSync --source /data --replica /mnt/mirror --interval 00:05:00 --log /var/log/foldersync.log"
        );
    }
}