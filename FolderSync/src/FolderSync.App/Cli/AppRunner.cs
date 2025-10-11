using FolderSync.App.Cli.Interfaces;
using FolderSync.Core.Application;
using FolderSync.Core.Configuration;
using FolderSync.Core.Logging;

namespace FolderSync.App.Cli;

public class AppRunner
{
    private readonly string[] _args;
    private readonly IArgsParser _argsParser;
    private readonly LoggingConfigurator _loggerConfigurator;
    private readonly Func<SyncOptions, SyncLoop> _syncLoopFactory;

    public AppRunner(string[] args,
        IArgsParser argsParser,
        Func<SyncOptions, SyncLoop> syncLoopFactory, LoggingConfigurator loggerConfigurator)
    {
        _args = args;
        _argsParser = argsParser;
        _syncLoopFactory = syncLoopFactory;
        _loggerConfigurator = loggerConfigurator;
    }

    public async Task<int> RunAsync()
    {
        try
        {
            if (_argsParser.IsHelpRequested(_args))
            {
                _argsParser.PrintUsage(Console.Out);
                return (int)ExitCode.Success;
            }

            var opts = _argsParser.Parse(_args);
            LoggingConfigurator.Apply(opts.LogFilePath, opts.IsDebug);
            var syncLoop = _syncLoopFactory(opts);


            await syncLoop.RunAsync(CancellationToken.None);

            return (int)ExitCode.Success;
        }
        catch (ArgumentException ex)
        {
            await Console.Error.WriteLineAsync($"Invalid arguments: {ex.Message}");
            await Console.Error.WriteLineAsync();
            _argsParser.PrintUsage(Console.Error);
            return (int)ExitCode.InvalidArguments;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Unexpected error: {ex}");
            return (int)ExitCode.UnhandledException;
        }
    }
}