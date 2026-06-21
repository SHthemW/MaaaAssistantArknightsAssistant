using System.Net.Http;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Game_Daily_Routine_Launcher;

public sealed class WebhookRelayService : IDisposable
{
    private const string WeComWebhookHost = "https://qyapi.weixin.qq.com";
    private const string WeComWebhookPath = "/cgi-bin/webhook/send";

    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IHost? _host;

    public bool IsRunning => _host is not null;
    public int Port { get; private set; }

    public async Task<WebhookRelayResult> StartAsync(int port, Func<string, string?, Task>? logAsync = null)
    {
        await _gate.WaitAsync();
        try
        {
            if (IsRunning && Port == port)
                return new WebhookRelayResult(true, $"Webhook 中转已运行，端口 {port}。");

            await StopCoreAsync().ConfigureAwait(false);

            var builder = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.ListenAnyIP(port);
                    });
                    webBuilder.Configure(app =>
                    {
                        app.Run(context => HandleRequestAsync(context, logAsync ?? ((_, _) => Task.CompletedTask)));
                    });
                });

            try
            {
                _host = builder.Build();
                await _host.StartAsync();
                Port = port;
                return new WebhookRelayResult(true, $"Webhook 中转已启动，监听端口 {port}。");
            }
            catch (Exception ex)
            {
                _host = null;
                return new WebhookRelayResult(false, $"Webhook 中转启动失败：{ex.Message}");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await StopCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task StopCoreAsync()
    {
        if (_host is null)
            return;

        try
        {
            await _host.StopAsync();
        }
        catch
        {
        }
        finally
        {
            _host.Dispose();
            _host = null;
        }
    }

    private async Task HandleRequestAsync(HttpContext context, Func<string, string?, Task> logAsync)
    {
        try
        {
            if (!HttpMethods.IsPost(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                await context.Response.WriteAsync("Only POST is supported.");
                return;
            }

            var compatibleRequest = IsWeComCompatibleRequest(context.Request);
            string url;
            string forwardedBody;
            string? parseLog = null;

            if (compatibleRequest)
            {
                url = BuildWeComForwardUrl(context.Request);
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                forwardedBody = await reader.ReadToEndAsync();
                parseLog = "Webhook 中转使用企业微信兼容模式，已原样转发请求 Body。";
            }
            else
            {
                var wrappedRequest = await ReadWrappedRequestAsync(context, logAsync);
                if (!wrappedRequest.Success)
                    return;

                url = wrappedRequest.Url;
                forwardedBody = wrappedRequest.Body;
                parseLog = wrappedRequest.ParseLog;
            }

            await logAsync($"Webhook 中转接收：url={url}", forwardedBody);
            if (!string.IsNullOrWhiteSpace(parseLog))
                await logAsync(parseLog, forwardedBody);

            var result = await ForwardAsync(url, forwardedBody);
            await logAsync(result.Message, BuildForwardLogBody(result));

            if (!result.Success)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                await context.Response.WriteAsync("Forward failed.");
                return;
            }

            await context.Response.WriteAsync("OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Webhook relay error: {ex.Message}");
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync(ex.Message);
            }
        }
    }

    private static bool IsWeComCompatibleRequest(HttpRequest request)
        => request.Path.Equals(WeComWebhookPath, StringComparison.OrdinalIgnoreCase) &&
           request.Query.ContainsKey("key");

    private static string BuildWeComForwardUrl(HttpRequest request)
        => $"{WeComWebhookHost}{request.Path}{request.QueryString}";

    private static async Task<RelayRequestReadResult> ReadWrappedRequestAsync(
        HttpContext context,
        Func<string, string?, Task> logAsync)
    {
        string url;
        JsonElement body;

        try
        {
            using var document = await JsonDocument.ParseAsync(context.Request.Body);
            var root = document.RootElement;

            if (!root.TryGetProperty("url", out var urlElement) || urlElement.ValueKind != JsonValueKind.String)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Missing url.");
                return RelayRequestReadResult.Failed();
            }

            if (!root.TryGetProperty("body", out var bodyElement))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Missing body.");
                return RelayRequestReadResult.Failed();
            }

            url = urlElement.GetString() ?? string.Empty;
            body = bodyElement.Clone();
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync($"Invalid request: {ex.Message}");
            return RelayRequestReadResult.Failed();
        }

        if (TryResolveForwardedBody(body, out var forwardedBody, out var parseLog, out var parseError))
            return RelayRequestReadResult.Succeeded(url, forwardedBody, parseLog);

        var errorMessage = $"Webhook 中转转发失败：body 字符串不是有效 JSON，原因：{parseError}";
        await logAsync(errorMessage, body.GetRawText());
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Invalid body JSON.");
        return RelayRequestReadResult.Failed();
    }

    private sealed record RelayRequestReadResult(bool Success, string Url, string Body, string? ParseLog)
    {
        public static RelayRequestReadResult Succeeded(string url, string body, string? parseLog)
            => new(true, url, body, parseLog);

        public static RelayRequestReadResult Failed()
            => new(false, string.Empty, string.Empty, null);
    }

    private static bool TryResolveForwardedBody(
        JsonElement body,
        out string forwardedBody,
        out string? parseLog,
        out string? parseError)
    {
        parseLog = null;
        parseError = null;

        if (body.ValueKind != JsonValueKind.String)
        {
            forwardedBody = body.GetRawText();
            return true;
        }

        var bodyText = body.GetString() ?? string.Empty;
        try
        {
            using var document = JsonDocument.Parse(bodyText);
            forwardedBody = document.RootElement.GetRawText();
            parseLog = "Webhook 中转已将字符串 Body 解析为 JSON，并使用解析后的 JSON 转发。";
            return true;
        }
        catch (JsonException ex)
        {
            forwardedBody = string.Empty;
            parseError = ex.Message;
            return false;
        }
    }

    private async Task<WebhookRelayResult> ForwardAsync(string url, string bodyText)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(bodyText, Encoding.UTF8, "application/json")
            };

            using var response = await _httpClient.SendAsync(request);
            return new WebhookRelayResult(
                response.IsSuccessStatusCode,
                response.IsSuccessStatusCode
                    ? $"Webhook 中转转发成功：url={url}，状态 {response.StatusCode}。"
                    : $"Webhook 中转转发失败：url={url}，状态 {response.StatusCode}。",
                url,
                bodyText);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Webhook relay forward failed: {ex.Message}");
            return new WebhookRelayResult(false, $"Webhook 中转转发失败：url={url}，原因：{ex.Message}", url, bodyText);
        }
    }

    private static string BuildForwardLogBody(WebhookRelayResult result)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(result.ForwardedUrl))
            builder.AppendLine($"实际转发URL：{result.ForwardedUrl}");

        if (!string.IsNullOrWhiteSpace(result.ForwardedBody))
        {
            builder.AppendLine("实际转发Body：");
            builder.AppendLine(result.ForwardedBody);
        }

        return builder.ToString().TrimEnd();
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _httpClient.Dispose();
        _gate.Dispose();
    }
}
