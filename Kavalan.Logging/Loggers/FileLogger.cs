using System.Diagnostics;
using System.Text;

namespace Kavalan.Logging;
public class FileLogger : BaseLogger, ILogger
{
    private string _autoLogFilePath;
    private string autoLogFilePath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_autoLogFilePath))
            {
                string fileName = Path.GetFileName(Environment.ProcessPath) ?? "EmptyExeName";
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                return Path.Combine(logDirectory, $"{DateTime.Now:dd_MM_yyyy}_{fileName}.log");
            }
            else
                return _autoLogFilePath;
        }
    }
    private static readonly SemaphoreSlim logSemaphore = new(1, 1);

    public FileLogger(LogLevel loggerLevel, string logFilePath = "", CancellationToken cancellationToken = default) : base(loggerLevel, cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(logFilePath))
            _autoLogFilePath = logFilePath;

        string? directoryPath = Path.GetDirectoryName(autoLogFilePath);
        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath ?? throw new Exception($"Invalid path: {autoLogFilePath}"));
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
                await File.AppendAllTextAsync(autoLogFilePath, base.GetLogEntryHeader(messageLoggerLevel, correlationId) + " " + base.GetLogEntryMessage(entry) + Environment.NewLine, Encoding.UTF8);
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