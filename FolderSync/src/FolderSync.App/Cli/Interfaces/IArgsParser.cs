using FolderSync.Core.Configuration;

namespace FolderSync.App.Cli.Interfaces;

public interface IArgsParser
{
    SyncOptions Parse(string[] args);
    bool IsHelpRequested(string[] args);
    void PrintUsage(TextWriter output);
}