using FolderSync.Core.Application;
using FolderSync.Core.Configuration;
using FolderSync.Core.Logging;

namespace FolderSync.App.Cli;

public class AppRunner
{
    private readonly string[] _args;
    private readonly ArgsLexer _lexer;
    private readonly Func<SyncOptions, SyncLoop> _syncLoopFactory;
    private readonly ArgsValidator _validator;

    public AppRunner(string[] args,
        ArgsLexer argsLexer,
        Func<SyncOptions, SyncLoop> syncLoopFactory, ArgsValidator validator)
    {
        _args = args;
        _lexer = argsLexer;
        _syncLoopFactory = syncLoopFactory;
        _validator = validator;
    }

    public async Task<int> RunAsync()
    {
        if (HasHelpArg(_args))
        {
            await PrintUsageAsync(Console.Out);
            return (int)ExitCode.Success;
        }

        if (_args.Length == 0)
        {
            await Console.Error.WriteLineAsync("Missing arguments.");
            await Console.Error.WriteLineAsync();
            await PrintUsageAsync(Console.Error);
            return (int)ExitCode.InvalidArguments;
        }

        var cts = new CancellationTokenSource();
        var ctsForHandler = cts;
        ConsoleCancelEventHandler handler;

        handler = (_, e) =>
        {
            e.Cancel = true;
            if (!ctsForHandler.IsCancellationRequested)
                ctsForHandler.Cancel();
        };

        Console.CancelKeyPress += handler;


        try
        {
            var parsedArgs = _lexer.ToParsedArgs(_args);
            var opts = _validator.ValidateArgs(parsedArgs);
            LoggingConfigurator.Apply(opts.LogFilePath, opts.IsDebug);
            var syncLoop = _syncLoopFactory(opts);

            await syncLoop.RunAsync(cts.Token);

            return (int)ExitCode.Success;
        }
        catch (ArgumentException ex)
        {
            await Console.Error.WriteLineAsync($"Invalid arguments: {ex.Message}");
            await Console.Error.WriteLineAsync();
            await PrintUsageAsync(Console.Error);
            return (int)ExitCode.InvalidArguments;
        }
        catch (OperationCanceledException)
        {
            return (int)ExitCode.Success;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Unexpected error: {ex}");
            return (int)ExitCode.UnhandledException;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
            cts.Dispose();
        }
    }

    private static bool HasHelpArg(string[] args)
    {
        return args.Any(a => string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(a, "/?", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task PrintUsageAsync(TextWriter output)
    {
        await output.WriteLineAsync(
            @"Usage:
            FolderSync --source <dir> --replica <dir> --interval <seconds|HH:MM:SS> --log <file>

            Examples:
            FolderSync --source C:\Data --replica D:\Mirror --interval 300 --log C:\logs\sync.log
            FolderSync --source /data --replica /mnt/mirror --interval 00:05:00 --log /var/log/foldersync.log"
        );
    }
}