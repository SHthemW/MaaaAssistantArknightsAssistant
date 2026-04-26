using System.Diagnostics;

namespace Game_Daily_Routine_Launcher;

public static class SystemService
{
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

    public static (bool success, string message) RegisterAutoRun(string taskName = "GameDailyRoutineLauncher")
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath == null)
            return (false, "无法获取当前程序路径");

        var args = $"/Create /TN \"{taskName}\" /TR \"\\\"{exePath}\\\" --autorun\" /SC ONLOGON /RL LIMITED /F";

        return RunSchtasks(args);
    }

    public static (bool success, string message) UnregisterAutoRun(string taskName = "GameDailyRoutineLauncher")
    {
        return RunSchtasks($"/Delete /TN \"{taskName}\" /F");
    }

    public static bool IsAutoRunRegistered(string taskName = "GameDailyRoutineLauncher")
    {
        var (success, _) = RunSchtasks($"/Query /TN \"{taskName}\"");
        return success;
    }

    private static (bool success, string message) RunSchtasks(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(psi);
            if (process == null)
                return (false, "无法启动 schtasks.exe");

            var stdout = process.StandardOutput.ReadToEnd().Trim();
            var stderr = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit(5000);

            var output = !string.IsNullOrEmpty(stdout) ? stdout : stderr;
            return (process.ExitCode == 0, output);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
