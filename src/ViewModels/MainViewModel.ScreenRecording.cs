using System.IO;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    private bool TryStartScreenRecording()
    {
        if (!ScreenRecordingEnabled)
            return false;

        try
        {
            var path = _screenRecordingService.Start(BuildScreenRecordingConfig());
            AddLog($"录屏已开始：{path}");
            return true;
        }
        catch (Exception ex)
        {
            RuntimeLogService.WriteException("启动录屏失败", ex);
            AddLog($"启动录屏失败：{ex.Message}");
            return false;
        }
    }

    private async Task StopScreenRecordingAsync()
    {
        if (!_screenRecordingService.IsRecording)
            return;

        try
        {
            var path = await _screenRecordingService.StopAsync();
            if (!string.IsNullOrWhiteSpace(path))
            {
                var relativePath = Path.GetRelativePath(
                    AppDomain.CurrentDomain.BaseDirectory,
                    path);
                AddLog($"录屏已保存：{relativePath}");
            }
        }
        catch (TimeoutException ex)
        {
            RuntimeLogService.WriteException("等待录屏文件完成超时", ex);
            AddLog("录屏停止超时，文件可能仍在写入。");
        }
        catch (Exception ex)
        {
            RuntimeLogService.WriteException("停止录屏失败", ex);
            AddLog($"停止录屏失败：{ex.Message}");
        }
    }
}
