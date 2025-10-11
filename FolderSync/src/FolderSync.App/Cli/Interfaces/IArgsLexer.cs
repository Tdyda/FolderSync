using FolderSync.Core.Configuration;

namespace FolderSync.App.Cli.Interfaces;

public interface IArgsLexer
{
    ParsedArgs ToParsedArgs(string[] args);
    bool IsHelpRequested(string[] args);
}