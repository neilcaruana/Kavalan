using System.Diagnostics;

namespace Kavalan.Logging;
public static class LoggerFactory
{
   
    public static ILogger CreateDefaultCompositeLogger(CancellationToken cancellationToken = default)
    {
        return new CompositeLogger([new FileLogger(LogLevel.Debug, string.Empty, cancellationToken),
                                    new ConsoleLogger(LogLevel.Debug, cancellationToken)]);
    }
    public static ILogger CreateCompositeLogger(LogLevel logLevel, CancellationToken cancellationToken)
    {
        return new CompositeLogger([new FileLogger(logLevel, string.Empty, cancellationToken),
                                    new ConsoleLogger(logLevel, cancellationToken)]);
    }
}
