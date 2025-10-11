using System.Globalization;
using FolderSync.App.Cli.Interfaces;
using FolderSync.Core.Configuration;

namespace FolderSync.App.Cli;

public class ArgsValidator : IArgsValidator
{
    private readonly IPathNormalizer _normalizer;

    public ArgsValidator(IPathNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public SyncOptions ValidateArgs(ParsedArgs parsedArgs)
    {
        var source = Require(parsedArgs.Flags, "--source");
        var replica = Require(parsedArgs.Flags, "--replica");
        var intervalStr = Require(parsedArgs.Flags, "--interval");
        var logPath = Require(parsedArgs.Flags, "--log");
        var isDebug = parsedArgs.Switches.Contains("--debug");

        var interval = ParseInterval(intervalStr);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("--interval must be positive.");

        var normSource = _normalizer.NormalizeExistingDirectory(source, true, "--source");
        var normReplica = _normalizer.NormalizeDirectory(replica, "--replica");
        var normLog = _normalizer.NormalizeFilePath(logPath, "--log");

        return new SyncOptions
        {
            SourcePath = normSource,
            ReplicaPath = normReplica,
            Interval = interval,
            LogFilePath = normLog,
            IsDebug = isDebug
        };
    }

    private static string Require(Dictionary<string, string> dict, string key)
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