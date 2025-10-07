using System.Globalization;
using FolderSync.Core.Configuration;
using FolderSync.Core.Scanning;

namespace FolderSync.App.Cli;

public class ArgsValidator
{
    public static SyncOptions ValidateArgs(Dictionary<string, string> dict)
    {
        var source = Require(dict, "--source");
        var replica = Require(dict, "--replica");
        var intervalStr = Require(dict, "--interval");
        var logPath = Require(dict, "--log");
        var isDebug = dict.Any(a => a.Key == "--debug" && a.Value == "true");

        var interval = ParseInterval(intervalStr);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentException("--interval must be positive.");
        
        string normSource = PathNormalizer.NormalizeExistingDirectory( source, mustExist: true, name: "--source");
        string normReplica = PathNormalizer.NormalizeDirectory(replica, name: "--replica");
        string normLog = PathNormalizer.NormalizeFilePath(logPath, name: "--log");

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
            throw new ArgumentException($"Lack of required flag {key}");
        return value.Trim();
    }

    private static TimeSpan ParseInterval(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds))
        {
            if (seconds <= 0) throw new ArgumentException("--interval in seconds must be greater than 0");
            return TimeSpan.FromSeconds(seconds);
        }

        if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var ts))
            return ts;
        throw new ArgumentException("Incorrect format --interval. Use seconds or HH:MM:SS.");
    }
}