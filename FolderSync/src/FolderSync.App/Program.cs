using System.IO.Abstractions;
using FolderSync.App.Cli;
using FolderSync.App.Interfaces;
using FolderSync.Core.Application;
using FolderSync.Core.Configuration;
using FolderSync.Core.Interfaces;
using FolderSync.Core.Sync.Diff;
using FolderSync.Core.Sync.Operations;
using FolderSync.Core.Sync.Scanning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace FolderSync.App;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        using var host = Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices(s =>
            {
                s.AddSingleton(args);

                s.AddSingleton<IArgsValidator, ArgsValidator>();
                s.AddSingleton<IArgsLexer, ArgsLexer>();
                s.AddSingleton<IPathNormalizer, PathNormalizer>();

                s.AddSingleton<IFileSystem, FileSystem>();
                s.AddSingleton<IFileCopier, FileCopier>();

                s.AddSingleton<DirectoryScanner>();
                s.AddSingleton<DiffEngine>();
                s.AddSingleton<CopyEngine>();
                s.AddSingleton<DeletionEngine>();
                s.AddSingleton<SyncRunner>();

                s.AddSingleton<Func<SyncOptions, SyncLoop>>(sp => opts =>
                {
                    var runner = sp.GetRequiredService<SyncRunner>();
                    return new SyncLoop(runner, opts.SourcePath, opts.ReplicaPath, opts.Interval);
                });

                s.AddSingleton<AppRunner>();
            })
            .Build();

        var app = host.Services.GetRequiredService<AppRunner>();
        return await app.RunAsync();
    }
}