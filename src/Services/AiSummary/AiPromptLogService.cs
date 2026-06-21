using System.IO;
using System.Text;

namespace Game_Daily_Routine_Launcher;

public sealed class AiPromptLogService
{
    private readonly string _logFilePath;

    public AiPromptLogService()
    {
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        _logFilePath = Path.Combine(logDir, "aiprompt.log");
    }

    public async Task WriteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return;

        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder();
        builder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
        builder.AppendLine(prompt.TrimEnd());

        await File.WriteAllTextAsync(_logFilePath, builder.ToString(), Encoding.UTF8, cancellationToken);
    }
}
