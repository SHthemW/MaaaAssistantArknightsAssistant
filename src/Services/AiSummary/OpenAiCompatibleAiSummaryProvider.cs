using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Game_Daily_Routine_Launcher;

public abstract class OpenAiCompatibleAiSummaryProvider : IAiSummaryProvider
{
    private static readonly HttpClient Client = new() { Timeout = Timeout.InfiniteTimeSpan };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public abstract AiSummaryProviderType ProviderType { get; }

    protected abstract string ProviderDisplayName { get; }

    public string BuildRequestBodyJson(AiSummaryConfig config, string prompt)
    {
        return JsonSerializer.Serialize(BuildPayload(config, prompt), JsonOptions);
    }

    public async Task<string> GenerateAsync(
        AiSummaryConfig config,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var providerConfig = GetProviderConfig(config);
        ValidateConfig(providerConfig);
        var apiUrl = AiSummaryEndpointResolver.Resolve(ProviderType, providerConfig.ApiUrl);

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", providerConfig.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(BuildPayload(config, prompt), JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await Client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw await CreateRequestExceptionAsync(response, cancellationToken);

        return config.Common.Stream
            ? await OpenAiCompatibleResponseParser.ReadStreamedContentAsync(
                response,
                ProviderDisplayName,
                cancellationToken)
            : OpenAiCompatibleResponseParser.ExtractMessageFromJson(
                await response.Content.ReadAsStringAsync(cancellationToken),
                ProviderDisplayName,
                response.Content.Headers.ContentType?.MediaType,
                response.RequestMessage?.RequestUri);
    }

    protected virtual Dictionary<string, object> BuildPayload(AiSummaryConfig config, string prompt)
    {
        var providerConfig = GetProviderConfig(config);
        return new Dictionary<string, object>
        {
            ["model"] = providerConfig.Model,
            ["messages"] = new[]
            {
                new { role = "system", content = config.Common.SystemPrompt },
                new { role = "user", content = prompt }
            },
            ["temperature"] = config.Common.Temperature,
            ["stream"] = config.Common.Stream
        };
    }

    protected abstract AiSummaryProviderConfig GetProviderConfig(AiSummaryConfig config);

    private void ValidateConfig(AiSummaryProviderConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new InvalidOperationException($"未配置{ProviderDisplayName} API Key。");

        if (string.IsNullOrWhiteSpace(config.ApiUrl))
            throw new InvalidOperationException($"未配置{ProviderDisplayName}接口地址。");

        if (string.IsNullOrWhiteSpace(config.Model))
            throw new InvalidOperationException($"未配置{ProviderDisplayName}模型。");
    }

    private async Task<HttpRequestException> CreateRequestExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = ExtractErrorMessage(body);
        var status = $"{(int)response.StatusCode} {response.ReasonPhrase}".Trim();
        var message = string.IsNullOrWhiteSpace(detail)
            ? $"{ProviderDisplayName}请求失败（{status}）。"
            : $"{ProviderDisplayName}请求失败（{status}）：{detail}";
        return new HttpRequestException(message, null, response.StatusCode);
    }

    private static string ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return string.Empty;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
                return message.GetString() ?? string.Empty;
        }
        catch (JsonException)
        {
        }

        return body.Length <= 500 ? body.Trim() : $"{body[..500].Trim()}...";
    }
}
