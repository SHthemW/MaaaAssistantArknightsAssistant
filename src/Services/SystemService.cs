using System.Diagnostics;

namespace Game_Daily_Routine_Launcher.Services;

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

    public static void RegisterAutoRun(string taskName = "GameDailyRoutineLauncher")
    {
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath == null) return;

        var args = $"/Create /TN \"{taskName}\" /TR \"\\\"{exePath}\\\" --autorun\" /SC ONLOGON /RL LIMITED /F";

        Process.Start(new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = args,
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    public static void UnregisterAutoRun(string taskName = "GameDailyRoutineLauncher")
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = $"/Delete /TN \"{taskName}\" /F",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    public static bool IsAutoRunRegistered(string taskName = "GameDailyRoutineLauncher")
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Query /TN \"{taskName}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = Process.Start(psi);
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
