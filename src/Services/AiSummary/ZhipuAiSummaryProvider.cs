using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Game_Daily_Routine_Launcher;

public sealed class ZhipuAiSummaryProvider : IAiSummaryProvider
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(60) };
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AiSummaryProviderType ProviderType => AiSummaryProviderType.ZhipuAi;

    public string BuildRequestBodyJson(ZhipuAiSummaryConfig config, string prompt)
    {
        return JsonSerializer.Serialize(BuildPayload(config, prompt), JsonOptions);
    }

    public async Task<string> GenerateAsync(ZhipuAiSummaryConfig config, string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new InvalidOperationException("未配置智谱AI API Key。");

        if (string.IsNullOrWhiteSpace(config.ApiUrl))
            throw new InvalidOperationException("未配置智谱AI接口地址。");

        if (string.IsNullOrWhiteSpace(config.Model))
            throw new InvalidOperationException("未配置智谱AI模型。");

        var payload = BuildPayload(config, prompt);

        using var request = new HttpRequestMessage(HttpMethod.Post, config.ApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        return config.Stream
            ? await ReadStreamedContentAsync(response, cancellationToken)
            : ExtractMessageFromJson(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private static async Task<string> ReadStreamedContentAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        var builder = new StringBuilder();

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                continue;

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]")
                break;

            builder.Append(ExtractDeltaContent(data));
        }

        var result = builder.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(result))
            return result;

        throw new InvalidOperationException("智谱AI返回结果中未找到内容。");
    }

    private static string ExtractDeltaContent(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0)
        {
            var choice = choices[0];

            if (choice.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("content", out var deltaContent))
                return deltaContent.GetString() ?? string.Empty;

            if (choice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var messageContent))
                return messageContent.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string ExtractMessageFromJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("choices", out var choices) &&
            choices.ValueKind == JsonValueKind.Array &&
            choices.GetArrayLength() > 0)
        {
            var choice = choices[0];

            if (choice.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
                return content.GetString() ?? string.Empty;
        }

        throw new InvalidOperationException("智谱AI返回结果中未找到内容。");
    }

    private static object BuildPayload(ZhipuAiSummaryConfig config, string prompt)
    {
        return new
        {
            model = config.Model,
            messages = new[]
            {
                new { role = "system", content = config.SystemPrompt },
                new { role = "user", content = prompt }
            },
            temperature = config.Temperature,
            stream = config.Stream
        };
    }
}
