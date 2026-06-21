using CommunityToolkit.Mvvm.Input;
using System.Text;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    [RelayCommand]
    private void TestAiSummary()
    {
        _ = Task.Run(TestAiSummaryAsync);
    }

    private async Task TestAiSummaryAsync()
    {
        if (!ShouldGenerateAiSummary(respectOnlyOnAutoRun: false))
        {
            AddLog("AI 智能总结未启用或当前平台不受支持。");
            return;
        }

        try
        {
            var config = BuildAiSummaryConfig();
            var prompt = "请用一句话确认当前 AI 总结接口是否可用。";
            var requestBody = _aiSummaryService.BuildRequestBodyJson(config, prompt);
            AddLog($"测试内容已发送: {prompt}");
            AddLog($"AI 测试请求体:\n{requestBody}");

            var summary = await _aiSummaryService.GenerateAsync(config, prompt, CancellationToken.None);
            AddLog(string.IsNullOrWhiteSpace(summary)
                ? "AI 测试完成，但未返回内容。"
                : $"AI 测试结果：{summary.Trim()}");
        }
        catch (Exception ex)
        {
            AddLog($"AI 测试失败：{ex.Message}");
        }
    }

    private async Task GenerateAndLogAiSummaryAsync()
    {
        if (!ShouldGenerateAiSummary())
            return;

        var config = BuildAiSummaryConfig();
        var prompt = BuildSummaryPrompt();
        if (string.IsNullOrWhiteSpace(prompt))
            return;

        try
        {
            await _aiPromptLogService.WriteAsync(prompt);
            var summary = await _aiSummaryService.GenerateAsync(config, prompt);
            if (string.IsNullOrWhiteSpace(summary))
            {
                AddLog("AI 总结返回为空。");
                return;
            }

            AddLog($"AI总结：{summary.Trim()}");
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
                Stream = ZhipuStream
            }
        };
    }

    private bool ShouldGenerateAiSummary(bool respectOnlyOnAutoRun = true)
    {
        if (!AiSummaryEnabled)
            return false;

        if (respectOnlyOnAutoRun && AiSummaryOnlyOnAutoRun && !App.IsAutoRun)
            return false;

        return _aiSummaryService.CanGenerate(BuildAiSummaryConfig());
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
