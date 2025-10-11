using FolderSync.Core.Configuration;

namespace FolderSync.App.Cli.Interfaces;

public interface IArgsValidator
{
    SyncOptions ValidateArgs(ParsedArgs parsedArgs);
}