using System.Diagnostics;

namespace Kavalan.Logging;
public static class LoggerFactory
{
    private static string logFilePath
    {
        get
        {
            ProcessModule? moduleHandle = Process.GetCurrentProcess().MainModule;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", $"{DateTime.Now.ToString("dd_MM_yyyy")}_{Path.GetFileName(moduleHandle?.FileName)}.log");
        }
    }
    public static ILogger CreateDefaultCompositeLogger(CancellationToken cancellationToken = default)
    {
        return new CompositeLogger([new FileLogger(LogLevel.Debug, logFilePath, cancellationToken),
                                    new ConsoleLogger(LogLevel.Debug, cancellationToken)]);
    }
    public static ILogger CreateCompositeLogger(LogLevel logLevel, CancellationToken cancellationToken)
    {
        return new CompositeLogger([new FileLogger(logLevel, logFilePath, cancellationToken),
                                    new ConsoleLogger(logLevel, cancellationToken)]);
    }
}
