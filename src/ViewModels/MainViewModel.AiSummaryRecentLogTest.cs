using System.IO;
using CommunityToolkit.Mvvm.Input;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    [RelayCommand]
    private async Task TestAiSummaryWithRecentLogAsync()
    {
        if (IsTestingAiSummary || !CanRunRecentAiSummaryTest)
            return;

        if (!ShouldGenerateAiSummary(respectOnlyOnAutoRun: false))
        {
            AiSummaryTestStatus = "测试失败：AI 智能总结未启用或当前平台不受支持。";
            AddLog("AI 智能总结未启用或当前平台不受支持。");
            return;
        }

        IsTestingAiSummary = true;
        AiSummaryTestStatus = "正在读取最近日志并等待服务器回应...";

        try
        {
            var prompt = await BuildPromptFromRecentLogAsync();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                HasRecentAiSummaryPromptLog = false;
                AiSummaryTestStatus = "测试失败：没有可用的最近日志。";
                AddLog("AI 最近日志测试失败：没有可用的最近日志。");
                return;
            }

            var config = BuildAiSummaryConfig();
            var requestBody = _aiSummaryService.BuildRequestBodyJson(config, prompt);
            AddLog($"使用最近日志测试内容已发送: {prompt}", pushWebhook: false);
            AddLog($"AI 最近日志测试请求体:\n{requestBody}", pushWebhook: false);

            var summary = await GenerateAiSummaryInBackgroundAsync(config, prompt, 60);
            if (string.IsNullOrWhiteSpace(summary))
            {
                AiSummaryTestStatus = "测试成功：服务器已回应，但未返回内容。";
                await LogFinalAiSummaryAsync("AI 最近日志测试完成，但未返回内容。");
                return;
            }

            AiSummaryTestStatus = "测试成功：服务器已回应。";
            await LogFinalAiSummaryAsync($"AI 最近日志测试结果：{summary.Trim()}");
        }
        catch (OperationCanceledException)
        {
            AiSummaryTestStatus = "测试超时：服务器 60 秒内未回应。";
            AddLog("AI 最近日志测试超时：服务器 60 秒内未回应。");
        }
        catch (Exception ex)
        {
            AiSummaryTestStatus = $"测试失败：{ex.Message}";
            AddLog($"AI 最近日志测试失败：{ex.Message}");
        }
        finally
        {
            IsTestingAiSummary = false;
            RefreshRecentAiSummaryPromptLogAvailability();
        }
    }

    private void RefreshRecentAiSummaryPromptLogAvailability()
    {
        HasRecentAiSummaryPromptLog = _aiPromptLogService.HasAvailablePromptLog();
    }

    private async Task<string?> BuildPromptFromRecentLogAsync()
    {
        var latestPrompt = await _aiPromptLogService.ReadLatestPromptAsync();
        if (string.IsNullOrWhiteSpace(latestPrompt))
            return null;

        var summaryPrompt = string.IsNullOrWhiteSpace(ZhipuSummaryPrompt)
            ? ZhipuAiSummaryConfig.DefaultSummaryPrompt
            : ZhipuSummaryPrompt;
        return ReplaceSummaryPrompt(latestPrompt, summaryPrompt);
    }

    private static string ReplaceSummaryPrompt(string prompt, string summaryPrompt)
    {
        var normalizedSummaryPrompt = string.IsNullOrWhiteSpace(summaryPrompt)
            ? ZhipuAiSummaryConfig.DefaultSummaryPrompt
            : summaryPrompt.TrimEnd();

        if (string.IsNullOrWhiteSpace(prompt))
            return normalizedSummaryPrompt;

        using var reader = new StringReader(prompt.Trim());
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                var remaining = reader.ReadToEnd().Trim();
                return string.IsNullOrWhiteSpace(remaining)
                    ? normalizedSummaryPrompt
                    : $"{normalizedSummaryPrompt}{Environment.NewLine}{Environment.NewLine}{remaining}";
            }
        }

        return normalizedSummaryPrompt;
    }
}
