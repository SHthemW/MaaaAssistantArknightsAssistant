using System.Diagnostics;
using System.Windows;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    private void ExecuteOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    private void AddLog(string message, string? rawBody = null, bool pushWebhook = true)
    {
        var entry = new LogEntryRecord(DateTime.Now, message)
        {
            RawBody = rawBody,
            Category = InferWebhookPushCategory(message, rawBody)
        };
        ExecuteOnUiThread(() => AppendLog(entry, pushWebhook));
    }

    private static WebhookPushContentCategory InferWebhookPushCategory(string message, string? rawBody)
    {
        if (message.Contains("AI总结：", StringComparison.Ordinal) ||
            message.Contains("AI 总结", StringComparison.Ordinal) ||
            message.Contains("AI 智能总结", StringComparison.Ordinal))
            return WebhookPushContentCategory.AiSummary;

        if (message.Contains("Webhook", StringComparison.Ordinal) || message.Contains("中转", StringComparison.Ordinal))
            return WebhookPushContentCategory.Webhook;

        if (message.Contains("静音", StringComparison.Ordinal) || message.Contains("音量", StringComparison.Ordinal))
            return WebhookPushContentCategory.Audio;

        if (message.Contains("亮度", StringComparison.Ordinal) || message.Contains("Twinkle Tray", StringComparison.Ordinal))
            return WebhookPushContentCategory.Brightness;

        if (message.Contains("开机自启", StringComparison.Ordinal) ||
            message.Contains("关机", StringComparison.Ordinal) ||
            message.Contains("设置", StringComparison.Ordinal))
            return WebhookPushContentCategory.System;

        if (message.Contains("任务链", StringComparison.Ordinal) ||
            message.Contains("任务", StringComparison.Ordinal) ||
            message.Contains("启动", StringComparison.Ordinal) ||
            message.Contains("完成", StringComparison.Ordinal) ||
            message.Contains("停止", StringComparison.Ordinal))
            return WebhookPushContentCategory.TaskExecution;

        if (message.Contains("监控", StringComparison.Ordinal) ||
            message.Contains("等待", StringComparison.Ordinal) ||
            message.Contains("超时", StringComparison.Ordinal) ||
            message.Contains("进程", StringComparison.Ordinal))
            return WebhookPushContentCategory.TaskMonitoring;

        return WebhookPushContentCategory.Other;
    }

    private void LoadWebhookPushContentOptions()
    {
        var enabled = new HashSet<WebhookPushContentCategory>(_appConfig.WebhookPushCategories);
        WebhookPushContentOptions.Clear();

        foreach (var option in CreateDefaultWebhookPushContentOptions())
        {
            var isEnabled = enabled.Count == 0 || enabled.Contains(option.Category);
            WebhookPushContentOptions.Add(new WebhookPushContentCategoryOptionViewModel(
                option.Category,
                option.DisplayName,
                isEnabled,
                SaveConfig));
        }
    }

    private static IReadOnlyList<(WebhookPushContentCategory Category, string DisplayName)> CreateDefaultWebhookPushContentOptions() =>
    [
        (WebhookPushContentCategory.TaskExecution, "任务启动/完成"),
        (WebhookPushContentCategory.TaskMonitoring, "进程监控"),
        (WebhookPushContentCategory.Audio, "静音/音量"),
        (WebhookPushContentCategory.Brightness, "亮度调节"),
        (WebhookPushContentCategory.Webhook, "Webhook 推送"),
        (WebhookPushContentCategory.AiSummary, "AI 总结"),
        (WebhookPushContentCategory.System, "系统设置"),
        (WebhookPushContentCategory.Other, "其他日志")
    ];

    private void AppendLog(LogEntryRecord entry, bool pushWebhook = true)
    {
        LogEntries.Add(entry);
        RuntimeLogService.WriteEntry(entry);
        Debug.WriteLine(entry.DisplayText);
        Console.WriteLine(entry.DisplayText);
        if (!string.IsNullOrWhiteSpace(entry.RawBody))
            Console.WriteLine(entry.RawBody);

        if (pushWebhook && ShouldPushLogEntry(entry))
            _ = PushWebhookAsync(entry.Message, entry.RawBody, entry.Timestamp.ToString("HH:mm:ss"));
    }

    private bool ShouldPushLogEntry(LogEntryRecord entry)
    {
        if (!WebhookEnabled || string.IsNullOrWhiteSpace(WebhookUrl))
            return false;

        if (WebhookOnlyPushAiSummary)
            return entry.Message.StartsWith("AI总结：", StringComparison.Ordinal);

        if (WebhookCustomPushContentEnabled)
            return WebhookPushContentOptions.Any(x => x.IsEnabled && x.Category == entry.Category);

        return true;
    }

    private async Task PushWebhookAsync(string message, string? rawBody, string time)
    {
        var content = string.IsNullOrWhiteSpace(rawBody)
            ? message
            : $"{message}\n原始Body：\n{rawBody}";
        var result = await WebhookService.SendAsync(WebhookUrl, WebhookBody, time, content);
        if (result.Success)
        {
            RuntimeLogService.WriteMessage($"Webhook sent: {result.RequestBody}");
            Debug.WriteLine($"Webhook sent: {result.RequestBody}");
        }
    }
}
