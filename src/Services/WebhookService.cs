using System.Net.Http;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Game_Daily_Routine_Launcher;

public static class WebhookService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static async Task<WebhookSendResult> SendAsync(string url, string bodyTemplate, string time, string content)
    {
        try
        {
            var body = bodyTemplate
                .Replace("__TIME__", EscapeJson(time))
                .Replace("__CONTENT__", EscapeJson(content));

            using var request = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await Client.PostAsync(url, request);
            return new WebhookSendResult(response.IsSuccessStatusCode, response.StatusCode.ToString(), body);
        }
        catch (Exception ex)
        {
            RuntimeLogService.WriteException("Webhook 发送失败", ex);
            Debug.WriteLine($"Webhook failed: {ex.Message}");
            return new WebhookSendResult(false, ex.Message, string.Empty);
        }
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

public sealed record WebhookSendResult(bool Success, string Message, string RequestBody);
