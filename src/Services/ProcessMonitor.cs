using System.Diagnostics;

namespace Game_Daily_Routine_Launcher;

public class ProcessMonitor : IDisposable
{
    private readonly string _processName;
    private readonly int _pollIntervalMs;
    private readonly CancellationTokenSource _cts = new();
    private Task? _monitorTask;

    public event Action? ProcessStarted;
    public event Action? ProcessExited;
    public event Action<string>? PhaseChanged;

    public bool IsActive { get; private set; }

    public ProcessMonitor(string processName, int pollIntervalMs = 60000)
    {
        _processName = processName;
        _pollIntervalMs = pollIntervalMs;
    }

    public void Start()
    {
        _monitorTask = Task.Run(() => MonitorLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts.Cancel();
        _monitorTask?.Wait(TimeSpan.FromSeconds(5));
    }

    private async Task MonitorLoop(CancellationToken ct)
    {
        var wasRunning = false;
        PhaseChanged?.Invoke("等待启动");

        while (!ct.IsCancellationRequested)
        {
            var isRunning = IsProcessRunning();

            if (isRunning && !wasRunning)
            {
                IsActive = true;
                PhaseChanged?.Invoke("等待关闭");
                ProcessStarted?.Invoke();
            }
            else if (!isRunning && wasRunning)
            {
                IsActive = false;
                ProcessExited?.Invoke();
                return;
            }

            wasRunning = isRunning;

            try
            {
                await Task.Delay(_pollIntervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private bool IsProcessRunning()
    {
        try
        {
            return Process.GetProcessesByName(_processName).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
