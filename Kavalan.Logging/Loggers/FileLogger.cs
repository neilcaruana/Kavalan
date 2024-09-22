using System.Text;

namespace Kavalan.Logging;
public class FileLogger : BaseLogger, ILogger
{
    private readonly string logFilePath = "";
    private static readonly SemaphoreSlim logSemaphore = new(1, 1);

    public FileLogger(LogLevel loggerLevel, string logFilePath = "", CancellationToken cancellationToken = default) : base(loggerLevel, cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logFilePath))
            this.logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "main.log");
        else
            this.logFilePath = logFilePath;

        string? directoryPath = Path.GetDirectoryName(this.logFilePath);
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath ?? throw new Exception($"Invalid path: {this.logFilePath}"));
    }
    public Task LogInfoAsync(string message, string correlationId = "") => LogToFileAsync(message, LogLevel.Info, correlationId);
    public Task LogErrorAsync(string errorMessage, Exception? exception = null, string correlationId = "") =>
        LogToFileAsync($"{errorMessage} {(exception != null ? $"{exception.GetBaseException().GetType().Name} : {exception.GetBaseException().Message}" : "")}", LogLevel.Error, correlationId);
    public Task LogRequestAsync(string message, string correlationId = "") => LogToFileAsync(base.CleanNonPrintableChars(message), LogLevel.Request, correlationId);
    public Task LogWarningAsync(string warningMessage, string correlationId = "") => LogToFileAsync(warningMessage, LogLevel.Warning, correlationId);
    public Task LogDebugAsync(string debugMessage, string correlationId = "") => LogToFileAsync(debugMessage, LogLevel.Debug, correlationId);
    private async Task LogToFileAsync(string entry, LogLevel messageLoggerLevel, string correlationId = "")
    {
        if (messageLoggerLevel <= LoggerLevel)
        {
            try
            {
                await logSemaphore.WaitAsync();
                await File.AppendAllTextAsync(logFilePath, base.GetLogEntryHeader(messageLoggerLevel, correlationId) + " " + base.GetLogEntryMessage(entry) + Environment.NewLine, Encoding.UTF8);
            }
            catch (OperationCanceledException) { }
            catch (AggregateException ) { }
            finally
            {
                if (logSemaphore.CurrentCount == 0)
                    logSemaphore.Release();
            }
        }
    }
}