using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Game_Daily_Routine_Launcher;

public static class SystemService
{
    private const string LegacyRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string DefaultTaskPrefix = "GameDailyRoutineLauncher";

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

    public static string GetAutoRunTaskName()
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "unknown";
        var fileName = Path.GetFileNameWithoutExtension(exePath);
        var normalized = string.IsNullOrWhiteSpace(fileName) ? "UnknownApp" : fileName;
        return $"{DefaultTaskPrefix}-{normalized}";
    }

    public static (bool success, string message) RegisterAutoRun() => RegisterAutoRun(GetAutoRunTaskName());

    public static (bool success, string message) RegisterAutoRun(string taskName)
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath == null)
            return (false, "无法获取当前程序路径。");

        var command = $"\"{exePath}\" --autorun";
        var createResult = RunSchtasksWithAutoElevate(
            "/Create",
            "/TN", taskName,
            "/TR", command,
            "/SC", "ONLOGON",
            "/RL", "LIMITED",
            "/F");

        if (!createResult.success)
            return createResult;

        var cleanupResult = CleanupLegacyAutoRunEntry(taskName);
        return cleanupResult.changed
            ? (true, $"已创建计划任务 {taskName}，并清理旧版注册表自启项。")
            : (true, $"已创建计划任务 {taskName}，登录时将自动启动。");
    }

    public static (bool success, string message) UnregisterAutoRun() => UnregisterAutoRun(GetAutoRunTaskName());

    public static (bool success, string message) UnregisterAutoRun(string taskName)
    {
        var deleteResult = RunSchtasksWithAutoElevate("/Delete", "/TN", taskName, "/F");
        var cleanupResult = CleanupLegacyAutoRunEntry(taskName);

        if (!deleteResult.success)
            return deleteResult;

        return cleanupResult.changed
            ? (true, $"已删除计划任务 {taskName}，并清理旧版注册表自启项。")
            : (true, $"已删除计划任务 {taskName}。");
    }

    public static bool IsAutoRunRegistered() => IsAutoRunRegistered(GetAutoRunTaskName());

    public static bool IsAutoRunRegistered(string taskName)
    {
        return IsScheduledTaskRegistered(taskName) || HasLegacyAutoRunEntry(taskName);
    }

    public static (bool changed, string? message) EnsureAutoRunUsesScheduledTask() => EnsureAutoRunUsesScheduledTask(GetAutoRunTaskName());

    public static (bool changed, string? message) EnsureAutoRunUsesScheduledTask(string taskName)
    {
        if (!HasLegacyAutoRunEntry(taskName))
            return (false, null);

        if (!IsScheduledTaskRegistered(taskName))
        {
            var migrated = RegisterAutoRun(taskName);
            return migrated.success
                ? (true, $"检测到旧版注册表自启项，已迁移为计划任务 {taskName}。")
                : (true, $"检测到旧版注册表自启项，但迁移失败：{migrated.message}");
        }

        var cleanupResult = CleanupLegacyAutoRunEntry(taskName);
        return cleanupResult.changed
            ? (true, $"检测到旧版注册表自启项，已自动清理。")
            : (false, null);
    }

    private static bool IsScheduledTaskRegistered(string taskName)
    {
        var result = RunSchtasks("/Query", "/TN", taskName);
        return result.success;
    }

    private static (bool success, string message) RunSchtasksWithAutoElevate(params string[] arguments)
    {
        var result = RunProcess("schtasks.exe", arguments);
        if (result.success)
            return result;

        if (!IsAccessDenied(result.message))
            return result;

        var elevatedResult = RunElevatedProcess("schtasks.exe", arguments);
        if (elevatedResult.success)
            return elevatedResult;

        return (false, AppendElevateHint(elevatedResult.message));
    }

    private static (bool success, string message) RunSchtasks(params string[] arguments)
    {
        return RunProcess("schtasks.exe", arguments);
    }

    private static (bool success, string message) RunProcess(string fileName, params string[] arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);
            if (process == null)
                return (false, $"无法启动 {fileName}。");

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            var message = NormalizeMessage(output, error);
            return process.ExitCode == 0
                ? (true, message)
                : (false, message);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static (bool success, string message) RunElevatedProcess(string fileName, params string[] arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = true,
                Verb = "runas",
                CreateNoWindow = true
            };

            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);
            if (process == null)
                return (false, $"无法以管理员权限启动 {fileName}。");

            process.WaitForExit();
            return process.ExitCode == 0
                ? (true, "已自动获取管理员权限并执行。")
                : (false, $"以管理员权限执行失败，退出码 {process.ExitCode}。");
        }
        catch (Win32Exception ex) when ((uint)ex.NativeErrorCode == 1223)
        {
            return (false, "用户取消了管理员权限请求。");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static bool IsAccessDenied(string message)
    {
        return message.Contains("拒绝访问", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Access is denied", StringComparison.OrdinalIgnoreCase);
    }

    private static string AppendElevateHint(string message)
    {
        return string.IsNullOrWhiteSpace(message)
            ? "执行失败，已尝试自动获取管理员权限。"
            : $"{message} 已尝试自动获取管理员权限。";
    }

    private static string NormalizeMessage(string output, string error)
    {
        var parts = new[] { output, error }.Where(text => !string.IsNullOrWhiteSpace(text)).Select(text => text.Trim());
        var message = string.Join(Environment.NewLine, parts);
        return string.IsNullOrWhiteSpace(message) ? "命令执行成功。" : message;
    }

    private static bool HasLegacyAutoRunEntry(string taskName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(LegacyRunKey);
            return key?.GetValue(taskName) != null;
        }
        catch
        {
            return false;
        }
    }

    private static (bool changed, string? message) CleanupLegacyAutoRunEntry(string taskName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(LegacyRunKey, writable: true);
            if (key?.GetValue(taskName) == null)
                return (false, null);

            key.DeleteValue(taskName);
            return (true, $"已清理旧版注册表自启项 {taskName}。");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
