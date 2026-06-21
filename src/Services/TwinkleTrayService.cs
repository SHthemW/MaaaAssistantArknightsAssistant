using System.Diagnostics;
using System.IO;

namespace Game_Daily_Routine_Launcher;

public sealed class TwinkleTrayService
{
    private const int LowestBrightness = 1;

    public TwinkleTrayAvailability DetectAvailability()
    {
        var process = FindTwinkleTrayProcess();
        if (process == null)
            return TwinkleTrayAvailability.Unavailable("未检测到正在运行的 Twinkle Tray 进程。");

        var exePath = GetExecutablePath(process);
        if (string.IsNullOrWhiteSpace(exePath))
            return TwinkleTrayAvailability.Unavailable("已检测到 Twinkle Tray，但无法获取其可执行文件路径。");

        if (!File.Exists(exePath))
            return TwinkleTrayAvailability.Unavailable($"Twinkle Tray 可执行文件不存在：{exePath}");

        return new TwinkleTrayAvailability(true, "Twinkle Tray 可用。", exePath, Array.Empty<TwinkleTrayMonitorState>());
    }

    public IReadOnlyList<TwinkleTrayMonitorState> CaptureCurrentStates()
    {
        var available = DetectAvailability();
        if (!available.IsAvailable || string.IsNullOrWhiteSpace(available.ExecutablePath))
            return Array.Empty<TwinkleTrayMonitorState>();

        var output = RunCapture(available.ExecutablePath, "--List");
        return ParseMonitorStates(output);
    }

    public async Task<TwinkleTrayCommandResult> SetAllLowestAsync(TwinkleTrayAvailability availability, CancellationToken cancellationToken = default)
    {
        if (!availability.IsAvailable || string.IsNullOrWhiteSpace(availability.ExecutablePath))
            return new TwinkleTrayCommandResult(false, availability.Message);

        var result = await RunCommandAsync(availability.ExecutablePath, $"--All --Set={LowestBrightness}", cancellationToken);
        return result.Success
            ? new TwinkleTrayCommandResult(true, "已将所有显示器亮度调至最低。")
            : result;
    }

    public async Task<TwinkleTrayCommandResult> RestoreAsync(TwinkleTrayAvailability availability, IReadOnlyList<TwinkleTrayMonitorState> originalStates, CancellationToken cancellationToken = default)
    {
        if (!availability.IsAvailable || string.IsNullOrWhiteSpace(availability.ExecutablePath))
            return new TwinkleTrayCommandResult(false, availability.Message);

        var errors = new List<string>();
        foreach (var monitor in originalStates)
        {
            var result = await RunCommandAsync(availability.ExecutablePath, monitor.BuildSetArguments(), cancellationToken);
            if (!result.Success)
                errors.Add(result.Message);
        }

        return errors.Count == 0
            ? new TwinkleTrayCommandResult(true, "已恢复显示器亮度。")
            : new TwinkleTrayCommandResult(false, string.Join("；", errors));
    }

    private static Process? FindTwinkleTrayProcess()
    {
        return Process.GetProcesses()
            .FirstOrDefault(p => p.ProcessName.Contains("twinkle", StringComparison.OrdinalIgnoreCase));
    }

    private static string? GetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<TwinkleTrayMonitorState> ParseMonitorStates(string output)
    {
        var states = new List<TwinkleTrayMonitorState>();
        var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var index = 1;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (!trimmed.Contains("Monitor", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.Contains("Display", StringComparison.OrdinalIgnoreCase))
                continue;

            states.Add(new TwinkleTrayMonitorState("MonitorNum", index.ToString(), ExtractBrightness(trimmed), trimmed));
            index++;
        }

        return states;
    }

    private static int ExtractBrightness(string line)
    {
        var digits = new string(line.Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var value))
            return Math.Clamp(value, LowestBrightness, 100);

        return LowestBrightness;
    }

    private static string RunCapture(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return string.Empty;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(10000);
            return string.IsNullOrWhiteSpace(output) ? process.StandardError.ReadToEnd() : output;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static async Task<TwinkleTrayCommandResult> RunCommandAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return new TwinkleTrayCommandResult(false, "无法启动 Twinkle Tray 命令。");

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            return process.ExitCode == 0
                ? new TwinkleTrayCommandResult(true, string.IsNullOrWhiteSpace(output) ? "命令执行成功。" : output.Trim())
                : new TwinkleTrayCommandResult(false, string.IsNullOrWhiteSpace(error) ? "命令执行失败。" : error.Trim());
        }
        catch (Exception ex)
        {
            return new TwinkleTrayCommandResult(false, ex.Message);
        }
    }
}
