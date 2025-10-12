using System.Globalization;
using FolderSync.Core.Configuration;

namespace FolderSync.App.Cli;

public class ArgsValidator(PathNormalizer pathNormalizer)
{
    public SyncOptions ValidateArgs(ParsedArgs parsedArgs)
    {
        var source = Require(parsedArgs.Tokens, "--source");
        var replica = Require(parsedArgs.Tokens, "--replica");
        var intervalStr = Require(parsedArgs.Tokens, "--interval");
        var logPath = Require(parsedArgs.Tokens, "--log");
        var isDebug = parsedArgs.Switches.Contains("--debug");

        var interval = ParseInterval(intervalStr);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("--interval must be positive.");

        var normSource = pathNormalizer.NormalizeExistingDirectory(source, true, "--source");
        var normReplica = pathNormalizer.NormalizeDirectory(replica, "--replica");
        var normLog = pathNormalizer.NormalizeFilePath(logPath, "--log");

        return new SyncOptions
        {
            SourcePath = normSource,
            ReplicaPath = normReplica,
            Interval = interval,
            LogFilePath = normLog,
            IsDebug = isDebug
        };
    }

    private static string Require(IReadOnlyDictionary<string, string> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"Lack of required token {key}");
        return value.Trim();
    }

    private static TimeSpan ParseInterval(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            if (seconds <= 0) throw new ArgumentException("--interval in seconds must be greater than 0");
            return TimeSpan.FromSeconds(seconds);
        }

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var ts))
            return ts;
        throw new ArgumentException("Incorrect format --interval. Use seconds or HH:MM:SS.");
    }
}