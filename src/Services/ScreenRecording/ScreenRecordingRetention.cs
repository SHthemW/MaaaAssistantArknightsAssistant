using System.IO;

namespace Game_Daily_Routine_Launcher;

public static class ScreenRecordingRetention
{
    public const int RetentionDays = 7;
    private const string TimestampFormat = "yyyyMMdd-HHmmss";

    public static void DeleteExpired(string directory, DateTime now)
    {
        if (!Directory.Exists(directory))
            return;

        try
        {
            var cutoff = now.AddDays(-RetentionDays);
            var expiredFiles = new DirectoryInfo(directory)
                .EnumerateFiles("*.mp4", SearchOption.TopDirectoryOnly)
                .Select(file => (File: file, Timestamp: GetTimestamp(file)))
                .Where(item => item.Timestamp < cutoff)
                .OrderByDescending(item => item.Timestamp)
                .ToList();

            foreach (var item in expiredFiles)
            {
                try
                {
                    item.File.Delete();
                }
                catch (IOException ex)
                {
                    RuntimeLogService.WriteException($"录屏文件清理失败：{item.File.FullName}", ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    RuntimeLogService.WriteException($"录屏文件清理失败：{item.File.FullName}", ex);
                }
            }
        }
        catch (IOException ex)
        {
            RuntimeLogService.WriteException("录屏文件清理失败", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            RuntimeLogService.WriteException("录屏文件清理失败", ex);
        }
    }

    private static DateTime GetTimestamp(FileInfo file)
    {
        var name = Path.GetFileNameWithoutExtension(file.Name);
        var timestampText = name.Length >= TimestampFormat.Length
            ? name[..TimestampFormat.Length]
            : name;
        return DateTime.TryParseExact(
                timestampText,
                TimestampFormat,
                null,
                System.Globalization.DateTimeStyles.None,
                out var timestamp)
            ? timestamp
            : file.LastWriteTime;
    }
}
