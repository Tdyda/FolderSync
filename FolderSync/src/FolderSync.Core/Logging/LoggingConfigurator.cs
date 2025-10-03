using Microsoft.Extensions.Logging;
using Serilog;

namespace FolderSync.Core.Logging;

public static class LoggingConfigurator
{
    public static ILoggerFactory Configure(string logFilePath)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
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