using Microsoft.Extensions.Logging;
using Serilog;

namespace FolderSync.Core.Orchestration;

public static class LoggingConfigurator
{
    public static ILoggerFactory Configure(string logFilePath, bool isDebug)
    {
        var level = isDebug ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Information;
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .WriteTo.Console()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Day,
                shared: true)
            .CreateLogger();
        
        return LoggerFactory.Create(b =>
        {
            b.ClearProviders();
            b.AddSerilog(Log.Logger, dispose: true);
        });
    }
}