using FolderSync.Core.Configuration;

namespace FolderSync.App.Interfaces;

public interface IArgsValidator
{
    SyncOptions ValidateArgs(ParsedArgs parsedArgs);
}