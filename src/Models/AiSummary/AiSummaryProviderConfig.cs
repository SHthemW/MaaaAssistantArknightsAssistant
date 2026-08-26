using System.Text.Json.Serialization;

namespace Game_Daily_Routine_Launcher;

public abstract class AiSummaryProviderConfig
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public AiSummaryProxyConfig Proxy { get; set; } = new();
}

public sealed class ZhipuAiSummaryConfig : AiSummaryProviderConfig
{
    public ZhipuAiSummaryConfig()
    {
        ApiUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions";
        Model = "glm-4.7-flash";
    }

    public bool ThinkingEnabled { get; set; } = true;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("systemPrompt")]
    public string? LegacySystemPrompt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("summaryPrompt")]
    public string? LegacySummaryPrompt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("temperature")]
    public double? LegacyTemperature { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("timeoutSeconds")]
    public int? LegacyTimeoutSeconds { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("requestRetryCount")]
    public int? LegacyRequestRetryCount { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("stream")]
    public bool? LegacyStream { get; set; }

    [JsonIgnore]
    public bool HasLegacyCommonConfig =>
        LegacySystemPrompt is not null ||
        LegacySummaryPrompt is not null ||
        LegacyTemperature.HasValue ||
        LegacyTimeoutSeconds.HasValue ||
        LegacyRequestRetryCount.HasValue ||
        LegacyStream.HasValue;

    public void ClearLegacyCommonConfig()
    {
        LegacySystemPrompt = null;
        LegacySummaryPrompt = null;
        LegacyTemperature = null;
        LegacyTimeoutSeconds = null;
        LegacyRequestRetryCount = null;
        LegacyStream = null;
    }
}

public sealed class ChatGptAiSummaryConfig : AiSummaryProviderConfig
{
    public ChatGptAiSummaryConfig()
    {
        ApiUrl = "https://api.openai.com/v1/chat/completions";
        Model = "gpt-4o-mini";
    }
}

public sealed class DeepSeekAiSummaryConfig : AiSummaryProviderConfig
{
    public DeepSeekAiSummaryConfig()
    {
        ApiUrl = "https://api.deepseek.com/chat/completions";
        Model = "deepseek-chat";
    }
}
