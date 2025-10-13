using Serilog;
using Serilog.Events;

namespace FolderSync.Core.Logging;

public static class LoggingConfigurator
{
    public static void Apply(string logFilePath, bool isDebug)
    {
        var level = isDebug
            ? LogEventLevel.Debug
            : LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(level)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, shared: true)
            .CreateLogger();
    }
}