using FolderSync.Core.Options;
using FolderSync.Core.Scanning;

namespace FolderSync.App.Cli;

public class OptionsBuilder
{
    public static SyncOptions BuildOpts(RawArgs args)
    {
        string normSource = PathNormalizer.NormalizeExistingDirectory( args.Source, mustExist: true, name: "--source");
        string normReplica = PathNormalizer.NormalizeDirectory(args.Replica, name: "--replica");
        string normLog = PathNormalizer.NormalizeFilePath(args.LogPath, name: "--log");
        var interval = args.Interval;

        return new SyncOptions
        {
            SourcePath = normSource,
            ReplicaPath = normReplica,
            Interval = interval,
            LogFilePath = normLog
        };
    }
}