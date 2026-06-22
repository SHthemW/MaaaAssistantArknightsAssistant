using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestAiSummaryCommand))]
    private bool _isTestingAiSummary;

    [ObservableProperty]
    private string _aiSummaryTestStatus = string.Empty;

    [RelayCommand(CanExecute = nameof(CanTestAiSummary))]
    private async Task TestAiSummaryAsync()
    {
        if (!ShouldGenerateAiSummary(respectOnlyOnAutoRun: false))
        {
            AiSummaryTestStatus = "测试失败：AI 智能总结未启用或当前平台不受支持。";
            AddLog("AI 智能总结未启用或当前平台不受支持。");
            return;
        }

        IsTestingAiSummary = true;
        AiSummaryTestStatus = "等待服务器回应...";

        try
        {
            var config = BuildAiSummaryConfig();
            var prompt = "请用一句话确认当前 AI 总结接口是否可用。";
            var requestBody = _aiSummaryService.BuildRequestBodyJson(config, prompt);
            AddLog($"测试内容已发送: {prompt}");
            AddLog($"AI 测试请求体:\n{requestBody}");

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var summary = await _aiSummaryService.GenerateAsync(config, prompt, timeoutCts.Token);
            if (string.IsNullOrWhiteSpace(summary))
            {
                AiSummaryTestStatus = "测试成功：服务器已回应，但未返回内容。";
                AddLog("AI 测试完成，但未返回内容。");
                return;
            }

            AiSummaryTestStatus = "测试成功：服务器已回应。";
            AddLog($"AI 测试结果：{summary.Trim()}");
        }
        catch (OperationCanceledException)
        {
            AiSummaryTestStatus = "测试超时：服务器 60 秒内未回应。";
            AddLog("AI 测试超时：服务器 60 秒内未回应。");
        }
        catch (Exception ex)
        {
            AiSummaryTestStatus = $"测试失败：{ex.Message}";
            AddLog($"AI 测试失败：{ex.Message}");
        }
        finally
        {
            IsTestingAiSummary = false;
        }
    }

    private bool CanTestAiSummary() => !IsTestingAiSummary;

    private async Task GenerateAndLogAiSummaryAsync(bool isAutoRunExecution)
    {
        var skipReason = GetAiSummarySkipReason(isAutoRunExecution);
        if (skipReason != null)
        {
            AddLog(skipReason);
            return;
        }

        var config = BuildAiSummaryConfig();
        var prompt = BuildSummaryPrompt();
        if (string.IsNullOrWhiteSpace(prompt))
            return;

        try
        {
            await _aiPromptLogService.WriteAsync(prompt);
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(config.ZhipuAi.TimeoutSeconds, 1)));
            var summary = await _aiSummaryService.GenerateAsync(config, prompt, timeoutCts.Token);
            if (string.IsNullOrWhiteSpace(summary))
            {
                AddLog("AI 总结返回为空。");
                return;
            }

            AddLog($"AI总结：{summary.Trim()}");
        }
        catch (OperationCanceledException)
        {
            AddLog($"AI 总结超时：服务器 {Math.Max(config.ZhipuAi.TimeoutSeconds, 1)} 秒内未回应。");
        }
        catch (Exception ex)
        {
            AddLog($"AI 总结失败：{ex.Message}");
        }
    }

    private AiSummaryConfig BuildAiSummaryConfig()
    {
        return new AiSummaryConfig
        {
            Provider = AiSummaryEnabled ? SelectedAiSummaryProvider : AiSummaryProviderType.Off,
            ZhipuAi = new ZhipuAiSummaryConfig
            {
                ApiKey = ZhipuApiKey,
                ApiUrl = ZhipuApiUrl,
                Model = ZhipuModel,
                SystemPrompt = ZhipuSystemPrompt,
                Temperature = ZhipuTemperature,
                TimeoutSeconds = Math.Max(ZhipuTimeoutSeconds, 1),
                Stream = ZhipuStream
            }
        };
    }

    private bool ShouldGenerateAiSummary(bool respectOnlyOnAutoRun = true) =>
        GetAiSummarySkipReason(!respectOnlyOnAutoRun || App.IsAutoRun) == null;

    private string? GetAiSummarySkipReason(bool isAutoRunExecution)
    {
        if (!AiSummaryEnabled)
            return "AI 智能总结未启用。";

        if (AiSummaryOnlyOnAutoRun && !isAutoRunExecution)
            return "AI 智能总结已设置为仅在自动运行时生效，本次手动任务链不生成总结。";

        if (!_aiSummaryService.CanGenerate(BuildAiSummaryConfig()))
            return "AI 智能总结当前平台不受支持。";

        return null;
    }

    private string BuildSummaryPrompt()
    {
        var today = DateTime.Today;
        var builder = new StringBuilder();

        builder.AppendLine("你需要根据下面的运行日志，简单总结每条任务完成情况。");
        builder.AppendLine("请使用简洁中文输出，逐条列出任务结论。");
        builder.AppendLine();
        builder.AppendLine("任务最终状态：");
        foreach (var task in Tasks)
            builder.AppendLine($"[{task.Id}] {task.Name}：{GetTaskStateLabel(task.State)}");

        builder.AppendLine();
        builder.AppendLine("今日运行日志：");
        foreach (var entry in LogEntries.Where(x => x.Timestamp.Date == today))
        {
            builder.AppendLine(entry.DisplayText);
            if (!string.IsNullOrWhiteSpace(entry.RawBody))
                builder.AppendLine($"原始Body：{entry.RawBody}");
        }

        return builder.ToString();
    }

    private static string GetTaskStateLabel(TaskState state)
    {
        return state switch
        {
            TaskState.Idle => "待机",
            TaskState.Launching => "启动中",
            TaskState.Running => "运行中",
            TaskState.MonitoringWaitStart => "等待启动监控",
            TaskState.MonitoringWaitStop => "等待结束监控",
            TaskState.Completed => "已完成",
            TaskState.TimedOut => "已超时",
            TaskState.Error => "出错",
            _ => "未知"
        };
    }
}
