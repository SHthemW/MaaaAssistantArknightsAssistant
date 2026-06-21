using System.Net.Http;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Game_Daily_Routine_Launcher;

public sealed class WebhookRelayService : IDisposable
{
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
                    return;
                }

                if (!root.TryGetProperty("body", out var bodyElement))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Missing body.");
                    return;
                }

                url = urlElement.GetString() ?? string.Empty;
                body = bodyElement.Clone();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync($"Invalid request: {ex.Message}");
                return;
            }

            var forwardedBody = body.GetRawText();
            await logAsync($"Webhook 中转接收：url={url}", forwardedBody);

            var result = await ForwardAsync(url, forwardedBody);
            await logAsync(result.Message, result.ForwardedBody);

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
                bodyText);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Webhook relay forward failed: {ex.Message}");
            return new WebhookRelayResult(false, $"Webhook 中转转发失败：url={url}，原因：{ex.Message}", bodyText);
        }
    }

    public void Dispose()
    {
        StopAsync().GetAwaiter().GetResult();
        _httpClient.Dispose();
        _gate.Dispose();
    }
}
