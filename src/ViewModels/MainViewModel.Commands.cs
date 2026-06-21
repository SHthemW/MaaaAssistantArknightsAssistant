using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Text;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    [RelayCommand(CanExecute = nameof(CanStartAll))]
    private async Task StartAllAsync()
    {
        SaveConfig();
        IsRunning = true;

        var enabledTasks = Tasks.Where(t => t.Enabled).Select(t => t.ToConfig()).ToList();
        if (enabledTasks.Count == 0)
        {
            AddLog("没有启用的任务。");
            IsRunning = false;
            return;
        }

        foreach (var task in Tasks)
            task.State = TaskState.Idle;

        _chainRunner = new TaskChainRunner(_audioService, PollIntervalSeconds * 1000);
        _chainRunner.LogMessage += msg => AddLog(msg);
        _chainRunner.TaskStateChanged += (id, state) =>
        {
            ExecuteOnUiThread(() =>
            {
                var vm = Tasks.FirstOrDefault(t => t.Id == id);
                if (vm != null && Enum.TryParse<TaskState>(state, out var parsed))
                    vm.State = parsed;
            });
        };

        AddLog("开始执行任务链...");
        var autoSummaryRequested = _autoSummaryRequested;
        _autoSummaryRequested = false;

        var completed = false;
        try
        {
            completed = await _chainRunner.RunChainAsync(enabledTasks, MuteOnStart);
        }
        finally
        {
            _chainRunner.Dispose();
            _chainRunner = null;
            IsRunning = false;
        }

        AddLog(completed ? "任务链执行完成。" : "任务链已停止。");

        if (completed && autoSummaryRequested)
            await GenerateAndLogAiSummaryAsync();
        else if (completed && WebhookOnlyPushAiSummary && !AiSummaryEnabled)
            _ = PushWebhookAsync("未开启AI总结服务", null, DateTime.Now.ToString("HH:mm:ss"));

        if (completed && ShutdownOnComplete && (!ShutdownOnlyOnAutoRun || autoSummaryRequested))
        {
            AddLog("所有任务已完成，10秒后关机。");
            SystemService.Shutdown();
        }
    }

    [RelayCommand(CanExecute = nameof(CanStopAll))]
    private void StopAll()
    {
        _chainRunner?.Stop();

        foreach (var task in Tasks)
            task.State = TaskState.Idle;

        AddLog("正在停止所有任务...");
    }

    [RelayCommand]
    private async Task StartSingleAsync(GameTaskViewModel? taskVm)
    {
        if (taskVm == null)
            return;

        SaveConfig();
        taskVm.State = TaskState.Running;
        AddLog($"单独启动 {taskVm.Name}...");

        var runner = new TaskChainRunner(_audioService, PollIntervalSeconds * 1000);
        runner.LogMessage += msg => AddLog(msg);
        runner.TaskStateChanged += (id, state) =>
        {
            ExecuteOnUiThread(() =>
            {
                if (Enum.TryParse<TaskState>(state, out var parsed))
                    taskVm.State = parsed;
            });
        };

        try
        {
            await runner.RunChainAsync([taskVm.ToConfig()], false);
        }
        finally
        {
            runner.Dispose();
        }
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _audioService.SetMute(IsMuted);
        _didAutoMute = false;
        AddLog(IsMuted ? "系统音量已静音。" : "系统音量已恢复。");
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogEntries.Clear();
    }

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        if (LogEntries.Count == 0)
        {
            AddLog("没有可导出的日志。");
            return;
        }

        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        var path = Path.Combine(logDir, $"runtime-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        var content = string.Join(Environment.NewLine, LogEntries.Select(x => x.DisplayText));
        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
        AddLog($"运行日志已导出：{path}");
    }

    [RelayCommand]
    private void TestWebhook()
    {
        _ = Task.Run(TestWebhookAsync);
    }

    private async Task TestWebhookAsync()
    {
        if (!WebhookEnabled || string.IsNullOrWhiteSpace(WebhookUrl))
        {
            AddLog("Webhook 未启用或 URL 为空。");
            return;
        }

        var testContent = "这是一条来自 MAAA 的测试消息。";
        var time = DateTime.Now.ToString("HH:mm:ss");
        var requestBody = WebhookBody
            .Replace("__TIME__", time)
            .Replace("__CONTENT__", testContent);

        AddLog($"测试内容已发送: {testContent}");
        AddLog($"Webhook 测试请求体:\n{requestBody}");
        await WebhookService.SendAsync(WebhookUrl, WebhookBody, time, testContent);
        AddLog("Webhook 测试请求已发送。");
    }
}
