using System.IO;
using ScreenRecorderLib;

namespace Game_Daily_Routine_Launcher;

public sealed partial class ScreenRecordingService
{
    private void ReleaseRecorder(Recorder recorder)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_recorder, recorder))
                return;

            recorder.OnRecordingComplete -= OnRecordingComplete;
            recorder.OnRecordingFailed -= OnRecordingFailed;
            recorder.Dispose();

            _recorder = null;
            _completionSource = null;
            _stopTask = null;
        }
    }

    private static string GetRecordDirectory() => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "logs",
        "records");

    private static string CreateUniqueFilePath(string directory, DateTime timestamp)
    {
        var baseName = timestamp.ToString("yyyyMMdd-HHmmss");
        var path = Path.Combine(directory, $"{baseName}.mp4");
        var suffix = 1;

        while (File.Exists(path))
            path = Path.Combine(directory, $"{baseName}-{suffix++:D2}.mp4");

        return path;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        try
        {
            StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            RuntimeLogService.WriteException("停止录屏失败", ex);
        }

        GC.SuppressFinalize(this);
    }
}
