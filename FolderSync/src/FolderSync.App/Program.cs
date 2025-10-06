using FolderSync.App;
using FolderSync.App.Cli;
using FolderSync.Core.Logging;

try
{
    if (ArgsParser.IsHelpRequested(args))
    {
        ArgsParser.PrintUsage(Console.Out);
        return (int)ExitCode.Success;
    }

    var opts = ArgsParser.Parse(args);
    using var loggerFactory = LoggingConfigurator.Configure(opts.LogFilePath, opts.IsDebug);
    (var loop, var cts) = Bootstrapper.Build(loggerFactory, opts);

    loop.RunAsync(cts.Token).GetAwaiter().GetResult();

    return (int)ExitCode.Success;
}
catch (ArgumentException ex)
{
    await Console.Error.WriteLineAsync($"Invalid arguments: {ex.Message}");
    await Console.Error.WriteLineAsync();
    ArgsParser.PrintUsage(Console.Error);
    return (int)ExitCode.InvalidArguments;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Unexpected error: {ex}");
    return (int)ExitCode.UnhandledException;
}