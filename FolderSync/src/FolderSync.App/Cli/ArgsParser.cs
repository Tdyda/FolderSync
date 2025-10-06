using System.Globalization;
using FolderSync.Core.Options;
using FolderSync.Core.Scanning;

namespace FolderSync.App.Cli;

public static class ArgsParser
{
    public static bool IsHelpRequested(string[] args) => ArgsLexer.IsHelpRequested(args);
    public static SyncOptions Parse(string[] args)
    {
        var dict = ArgsLexer.ToDictionary(args);
        var raw = ArgsValidator.ValidateArgs(dict);
        return OptionsBuilder.BuildOpts(raw);
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