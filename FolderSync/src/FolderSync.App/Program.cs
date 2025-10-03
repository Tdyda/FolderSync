using FolderSync.App;
using FolderSync.App.Cli;
using FolderSync.Core.Options;

try
{
    if (ArgsParser.IsHelpRequested(args))
    {
        ArgsParser.PrintUsage(Console.Out);
        return (int)ExitCode.Success;
    }

    SyncOptions opts = ArgsParser.Parse(args);
    
    Console.WriteLine("Argumenty Ok. Znormalizowane wartości:");
    Console.WriteLine(opts.ToString());

    return (int)ExitCode.Success;
}
catch (ArgumentException ex)
{
    await Console.Error.WriteLineAsync($"BŁĄD ARGUMENTÓW: {ex.Message}");
    await Console.Error.WriteLineAsync();
    ArgsParser.PrintUsage(Console.Error);
    return (int)ExitCode.InvalidArguments;
}
