using Microsoft.Extensions.Logging;

namespace GIANG.Dev.DiskAnalyzer.Infrastructure.Logging;

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDirectory;
    private readonly long _maxFileSizeBytes;

    public FileLoggerProvider(string? logDirectory = null, long maxFileSizeBytes = 10 * 1024 * 1024)
    {
        _maxFileSizeBytes = maxFileSizeBytes;
        _logDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GiangDevDiskAnalyzer", "Logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _logDirectory, _maxFileSizeBytes);
    }

    public void Dispose() { }
}

public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logDirectory;
    private readonly long _maxFileSizeBytes;
    private static readonly object FileLock = new();

    public FileLogger(string categoryName, string logDirectory, long maxFileSizeBytes)
    {
        _categoryName = categoryName;
        _logDirectory = logDirectory;
        _maxFileSizeBytes = maxFileSizeBytes;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        string message = formatter(state, exception);
        string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
        string filePath = Path.Combine(_logDirectory, $"analyzer-{dateStr}.log");

        string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] [{_categoryName}] {message}";
        if (exception != null)
        {
            logLine += Environment.NewLine + exception.ToString();
        }
        logLine += Environment.NewLine;

        lock (FileLock)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var fi = new FileInfo(filePath);
                    if (fi.Length > _maxFileSizeBytes)
                    {
                        // Rotate log
                        string archivePath = Path.Combine(_logDirectory, $"analyzer-{dateStr}-{DateTime.Now:HHmmss}.log");
                        File.Move(filePath, archivePath);
                    }
                }

                File.AppendAllText(filePath, logLine);
            }
            catch
            {
                // Fallback: never crash application due to logging
            }
        }
    }
}
