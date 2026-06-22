using System.Diagnostics;
using System.IO;
using System.Text;

namespace Game_Daily_Routine_Launcher;

public static class RuntimeLogService
{
    private const int MaxLogFileCount = 5;
    private const string LogFilePattern = "session-*.log";

    private static readonly object SyncRoot = new();
    private static string? _currentLogFilePath;

    public static string? CurrentLogFilePath => _currentLogFilePath;

    public static void Initialize()
    {
        lock (SyncRoot)
        {
            if (_currentLogFilePath != null)
                return;

            try
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);

                _currentLogFilePath = CreateSessionLogPath(logDir);
                File.WriteAllText(
                    _currentLogFilePath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] 程序启动。{Environment.NewLine}",
                    Encoding.UTF8);

                PruneOldSessionLogs(logDir);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Runtime log initialize failed: {ex}");
            }
        }
    }

    public static void WriteEntry(LogEntryRecord entry)
    {
        var text = string.IsNullOrWhiteSpace(entry.RawBody)
            ? $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.Message}"
            : $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.Message}{Environment.NewLine}{entry.RawBody}";

        WriteText(text);
    }

    public static void WriteMessage(string message) =>
        WriteText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");

    public static void WriteException(string message, Exception exception) =>
        WriteText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}{exception}");

    private static void WriteText(string text)
    {
        lock (SyncRoot)
        {
            try
            {
                if (_currentLogFilePath == null)
                    Initialize();

                if (_currentLogFilePath == null)
                    return;

                File.AppendAllText(_currentLogFilePath, text + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Runtime log write failed: {ex}");
            }
        }
    }

    private static string CreateSessionLogPath(string logDir)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        var path = Path.Combine(logDir, $"session-{timestamp}.log");
        if (!File.Exists(path))
            return path;

        for (var index = 1; index < 1000; index++)
        {
            path = Path.Combine(logDir, $"session-{timestamp}-{index}.log");
            if (!File.Exists(path))
                return path;
        }

        return Path.Combine(logDir, $"session-{timestamp}-{Guid.NewGuid():N}.log");
    }

    private static void PruneOldSessionLogs(string logDir)
    {
        var sessionLogs = Directory
            .GetFiles(logDir, LogFilePattern)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var deleteCount = sessionLogs.Count - MaxLogFileCount;
        if (deleteCount <= 0)
            return;

        var currentPath = _currentLogFilePath == null
            ? string.Empty
            : Path.GetFullPath(_currentLogFilePath);

        foreach (var filePath in sessionLogs.Take(deleteCount))
        {
            if (string.Equals(Path.GetFullPath(filePath), currentPath, StringComparison.OrdinalIgnoreCase))
                continue;

            File.Delete(filePath);
        }
    }
}
