using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Game_Daily_Routine_Launcher;

public sealed class TwinkleTrayService
{
    private const int LowestBrightness = 1;
    private static readonly TimeSpan CommandExitGracePeriod = TimeSpan.FromSeconds(2);

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
        var availability = DetectAvailability();
        if (!availability.IsAvailable || string.IsNullOrWhiteSpace(availability.ExecutablePath))
            return Array.Empty<TwinkleTrayMonitorState>();

        var output = RunCapture(availability.ExecutablePath, "--List");
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

        var results = await Task.WhenAll(originalStates.Select(monitor =>
            RunCommandAsync(availability.ExecutablePath, monitor.BuildSetArguments(), cancellationToken)));
        var errors = results.Where(result => !result.Success).Select(result => result.Message).ToList();

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
        var cleaned = StripAnsiEscapeSequences(output);
        var blocks = cleaned.Split(["\r\n\r\n", "\n\n", "\r\r"], StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var normalized = block.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            var monitorNum = ExtractValue(normalized, @"MonitorNum\s*:\s*(\d+)");
            var monitorId = ExtractValue(normalized, @"MonitorID\s*:\s*(.+)");
            var name = ExtractValue(normalized, @"Name\s*:\s*(.+)");
            var brightness = ExtractBrightness(normalized);

            if (string.IsNullOrWhiteSpace(monitorNum) || string.IsNullOrWhiteSpace(monitorId))
                continue;

            states.Add(new TwinkleTrayMonitorState(
                "MonitorID",
                monitorId,
                brightness,
                name));
        }

        return states;
    }

    private static string ExtractValue(string block, string pattern)
    {
        var match = Regex.Match(block, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static int ExtractBrightness(string block)
    {
        var match = Regex.Match(block, @"Brightness\s*:\s*(\d{1,3})", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
            return Math.Clamp(value, LowestBrightness, 100);

        return LowestBrightness;
    }

    private static string StripAnsiEscapeSequences(string text)
    {
        return Regex.Replace(text, @"\x1B\[[0-?]*[ -/]*[@-~]", string.Empty);
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
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = Process.Start(psi);
            if (process == null)
                return new TwinkleTrayCommandResult(false, "无法启动 Twinkle Tray 命令。");

            var exitTask = process.WaitForExitAsync(cancellationToken);
            var completedTask = await Task.WhenAny(exitTask, Task.Delay(CommandExitGracePeriod, cancellationToken));
            if (completedTask != exitTask)
                return new TwinkleTrayCommandResult(true, "命令已发送，Twinkle Tray 仍在后台处理。");

            await exitTask;

            return process.ExitCode == 0
                ? new TwinkleTrayCommandResult(true, "命令执行成功。")
                : new TwinkleTrayCommandResult(false, $"命令执行失败，退出码 {process.ExitCode}。");
        }
        catch (Exception ex)
        {
            return new TwinkleTrayCommandResult(false, ex.Message);
        }
    }
}
