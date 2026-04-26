using System.Diagnostics;
using Microsoft.Win32;

namespace Game_Daily_Routine_Launcher;

public static class SystemService
{
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string DefaultTaskName = "GameDailyRoutineLauncher";

    public static void Shutdown(int delaySeconds = 10)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = $"-s -t {delaySeconds}",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    public static void CancelShutdown()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = "-a",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    public static (bool success, string message) RegisterAutoRun(string taskName = DefaultTaskName)
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath == null)
            return (false, "无法获取当前程序路径");

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null)
                return (false, $"无法打开注册表项 HKCU\\{RunKey}");

            var command = $"\"{exePath}\" --autorun";
            key.SetValue(taskName, command);

            var saved = key.GetValue(taskName) as string;
            return saved == command
                ? (true, $"已写入注册表 HKCU\\{RunKey}\\{taskName} = {command}")
                : (false, "注册表写入后回读不一致");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static (bool success, string message) UnregisterAutoRun(string taskName = DefaultTaskName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            if (key == null)
                return (false, $"无法打开注册表项 HKCU\\{RunKey}");

            if (key.GetValue(taskName) == null)
                return (true, "注册表中不存在该项，无需删除");

            key.DeleteValue(taskName);
            return (true, $"已删除注册表项 HKCU\\{RunKey}\\{taskName}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static bool IsAutoRunRegistered(string taskName = DefaultTaskName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(taskName) != null;
        }
        catch
        {
            return false;
        }
    }
}
