using System.Diagnostics;

namespace Game_Daily_Routine_Launcher;

public class TaskChainRunner : IDisposable
{
    private readonly AudioService _audio;
    private readonly int _pollIntervalMs;
    private CancellationTokenSource? _cts;
    private ProcessMonitor? _currentMonitor;

    public event Action<string, string>? TaskStateChanged;
    public event Action<string>? LogMessage;
    public event Action? ChainCompleted;

    public bool IsRunning { get; private set; }

    public TaskChainRunner(AudioService audio, int pollIntervalMs = 60000)
    {
        _audio = audio;
        _pollIntervalMs = pollIntervalMs;
    }

    public async Task RunChainAsync(IReadOnlyList<GameTaskConfig> tasks, bool muteOnStart, bool shutdownOnComplete)
    {
        _cts = new CancellationTokenSource();
        IsRunning = true;

        try
        {
            if (muteOnStart)
            {
                _audio.SetMute(true);
                Log("系统音量已静音");
            }

            foreach (var task in tasks)
            {
                if (_cts.Token.IsCancellationRequested) break;
                if (!task.Enabled) continue;

                await RunSingleTaskAsync(task, _cts.Token);
            }

            if (!_cts.Token.IsCancellationRequested && shutdownOnComplete)
            {
                Log("所有任务已完成，10秒后关机...");
                SystemService.Shutdown();
            }
        }
        catch (OperationCanceledException)
        {
            Log("任务链已被用户停止");
        }
        finally
        {
            IsRunning = false;
            ChainCompleted?.Invoke();
        }
    }

    private async Task RunSingleTaskAsync(GameTaskConfig task, CancellationToken ct)
    {
        if (task.DelayBeforeStartMs > 0)
        {
            Log($"等待 {task.DelayBeforeStartMs / 1000} 秒后启动 {task.Name}...");
            TaskStateChanged?.Invoke(task.Id, "Launching");
            await Task.Delay(task.DelayBeforeStartMs, ct);
        }

        Log($"正在启动 {task.Name}...");
        TaskStateChanged?.Invoke(task.Id, "Running");

        LaunchTool(task);

        if (!string.IsNullOrEmpty(task.GameProcessName))
        {
            Log($"正在监控 {task.GameProcessName} 进程...");
            TaskStateChanged?.Invoke(task.Id, "Monitoring");

            await WaitForProcessExitAsync(task.GameProcessName, ct);

            Log($"{task.GameProcessName} 进程已退出");
        }

        TaskStateChanged?.Invoke(task.Id, "Completed");
        Log($"{task.Name} 已完成");
    }

    private static void LaunchTool(GameTaskConfig task)
    {
        var psi = new ProcessStartInfo();

        if (task.LaunchMode == LaunchMode.Uri)
        {
            psi.FileName = task.ToolPath;
            psi.UseShellExecute = true;
        }
        else
        {
            psi.FileName = task.ToolPath;
            psi.Arguments = task.ToolArgs;
            psi.UseShellExecute = true;
        }

        Process.Start(psi);
    }

    private async Task WaitForProcessExitAsync(string processName, CancellationToken ct)
    {
        using var monitor = new ProcessMonitor(processName, _pollIntervalMs);
        _currentMonitor = monitor;

        var tcs = new TaskCompletionSource();

        monitor.ProcessExited += () => tcs.TrySetResult();

        using var reg = ct.Register(() => tcs.TrySetCanceled());

        monitor.Start();

        await tcs.Task;

        _currentMonitor = null;
    }

    public void Stop()
    {
        _cts?.Cancel();
        _currentMonitor?.Stop();
    }

    private void Log(string message)
    {
        LogMessage?.Invoke(message);
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _currentMonitor?.Dispose();
        GC.SuppressFinalize(this);
    }
}
