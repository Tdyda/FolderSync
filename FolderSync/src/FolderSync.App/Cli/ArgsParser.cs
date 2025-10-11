using FolderSync.App.Cli.Interfaces;
using FolderSync.Core.Configuration;

namespace FolderSync.App.Cli;

public class ArgsParser : IArgsParser
{
    private readonly IArgsLexer _lexer;
    private readonly IArgsValidator _validator;

    public ArgsParser(IArgsLexer lexer, IArgsValidator validator)
    {
        _lexer = lexer;
        _validator = validator;
    }

    public bool IsHelpRequested(string[] args)
    {
        return _lexer.IsHelpRequested(args);
    }

    public SyncOptions Parse(string[] args)
    {
        var parsedArgs = _lexer.ToParsedArgs(args);
        return _validator.ValidateArgs(parsedArgs);
    }

    public void PrintUsage(TextWriter output)
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