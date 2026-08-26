using System.Net;
using System.Net.Http;

namespace Game_Daily_Routine_Launcher;

internal static class AiSummaryHttpClientFactory
{
    public static HttpClient Create(AiSummaryProxyConfig? proxyConfig)
    {
        if (proxyConfig is null || !proxyConfig.Enabled)
            return CreateDirectClient();

        var proxyUri = ParseProxyUri(proxyConfig.Url);
        var proxy = new WebProxy(proxyUri);
        if (!string.IsNullOrWhiteSpace(proxyConfig.Username))
        {
            proxy.Credentials = new NetworkCredential(
                proxyConfig.Username,
                proxyConfig.Password ?? string.Empty);
        }

        var handler = new HttpClientHandler
        {
            UseProxy = true,
            Proxy = proxy,
            PreAuthenticate = true
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    private static HttpClient CreateDirectClient()
    {
        var handler = new HttpClientHandler
        {
            UseProxy = false
        };

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    private static Uri ParseProxyUri(string? configuredUrl)
    {
        var value = configuredUrl?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new InvalidOperationException(
                "AI 代理地址必须是有效的 HTTP(S) 地址，例如 http://127.0.0.1:7890。");
        }

        return uri;
    }
}
