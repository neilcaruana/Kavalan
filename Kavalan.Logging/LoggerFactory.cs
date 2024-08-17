using System.Diagnostics;

namespace Kavalan.Logging;
public static class LoggerFactory
{
    public static ILogger CreateDefaultCompositeLogger(CancellationToken cancellationToken = default)
    {
        ProcessModule? ModuleHandle = Process.GetCurrentProcess().MainModule;
        return new CompositeLogger([new FileLogger(LogLevel.Debug, ModuleHandle?.FileName ?? "", cancellationToken),
                                        new ConsoleLogger(LogLevel.Debug, cancellationToken)]);
    }
    public static ILogger CreateCompositeLogger(LogLevel logLevel, CancellationToken cancellationToken)
    {
        ProcessModule? ModuleHandle = Process.GetCurrentProcess().MainModule;
        return new CompositeLogger([new FileLogger(logLevel, ModuleHandle?.FileName ?? "", cancellationToken),
                                        new ConsoleLogger(logLevel, cancellationToken)]);
    }
}
