using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace Game_Daily_Routine_Launcher;

public static class WebhookService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(10) };

    public static async void Send(string url, string bodyTemplate, string time, string content)
    {
        try
        {
            var body = bodyTemplate
                .Replace("__TIME__", EscapeJson(time))
                .Replace("__CONTENT__", EscapeJson(content));

            using var request = new StringContent(body, Encoding.UTF8, "application/json");
            await Client.PostAsync(url, request);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Webhook failed: {ex.Message}");
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
