using FolderSync.App;
using FolderSync.App.Cli;
using FolderSync.Core.Copying;
using FolderSync.Core.Diff;
using FolderSync.Core.Logging;
using FolderSync.Core.Options;
using FolderSync.Core.Scanning;
using Microsoft.Extensions.Logging;

try
{
    if (ArgsParser.IsHelpRequested(args))
    {
        ArgsParser.PrintUsage(Console.Out);
        return (int)ExitCode.Success;
    }

    SyncOptions opts = ArgsParser.Parse(args);

    using var loggerFactory = LoggingConfigurator.Configure(opts.LogFilePath);
    var logger = loggerFactory.CreateLogger("FolderSync");
    
    logger.LogInformation("Argumenty Ok. Znormalizowane wartości:");
    logger.LogInformation(opts.ToString());
    
    var scanner = new DirectoryScanner(loggerFactory.CreateLogger<DirectoryScanner>());
    var sourceSnap = await scanner.BuildSnapshotAsync(opts.SourcePath);
    var replicaSnap = await scanner.BuildSnapshotAsync(opts.ReplicaPath);
    var engine = new DiffEngine(loggerFactory.CreateLogger<DiffEngine>());
    var diffResult = engine.Compute(sourceSnap, replicaSnap);
    var copyEngine = new CopyEngine(loggerFactory.CreateLogger<CopyEngine>());
    await copyEngine.ApplyAsync(sourceSnap, replicaSnap, diffResult);
    
    return (int)ExitCode.Success;
}
catch (ArgumentException ex)
{
    await Console.Error.WriteLineAsync($"BŁĄD ARGUMENTÓW: {ex.Message}");
    await Console.Error.WriteLineAsync();
    ArgsParser.PrintUsage(Console.Error);
    return (int)ExitCode.InvalidArguments;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"NIEOCZEKIWANY BŁĄD: {ex}");
    return (int)ExitCode.UnhandledException;
}
