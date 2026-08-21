namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    private async Task<string> GenerateAiSummaryWithRetryAsync(AiSummaryConfig config, string prompt, int timeoutSeconds)
    {
        var retryCount = Math.Max(config.Common.RequestRetryCount, 0);
        Exception? lastException = null;

        for (var attempt = 0; attempt <= retryCount; attempt++)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 1)));
                return await _aiSummaryService.GenerateAsync(config, prompt, timeoutCts.Token);
            }
            catch (Exception ex) when (attempt < retryCount && AiSummaryRetryPolicy.ShouldRetry(ex))
            {
                lastException = ex;
                AddLog($"AI 总结请求失败，正在重试（{attempt + 1}/{retryCount}）：{GetRetryMessage(ex)}");
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        if (lastException != null)
            throw lastException;

        throw new InvalidOperationException("AI 总结请求失败。");
    }

    private async Task LogFinalAiSummaryAsync(string message, string? rawBody = null)
    {
        AddLog(message, rawBody, pushWebhook: false);
        await PushFinalAiSummaryWebhookAsync(message, rawBody);
    }

    private async Task PushFinalAiSummaryWebhookAsync(string message, string? rawBody)
    {
        if (!WebhookEnabled || string.IsNullOrWhiteSpace(WebhookUrl))
            return;

        await PushWebhookAsync(message, rawBody, DateTime.Now.ToString("HH:mm:ss"));
    }

    private static string GetRetryMessage(Exception ex)
    {
        return ex.Message.Trim();
    }
}
