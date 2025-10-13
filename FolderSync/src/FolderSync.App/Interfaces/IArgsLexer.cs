using FolderSync.Core.Configuration;

namespace FolderSync.App.Interfaces;

public interface IArgsLexer
{
    ParsedArgs ToParsedArgs(string[] args);
}