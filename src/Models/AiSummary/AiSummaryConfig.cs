namespace Game_Daily_Routine_Launcher;

public class AiSummaryConfig
{
    public AiSummaryProviderType Provider { get; set; } = AiSummaryProviderType.Off;

    public AiSummaryCommonConfig Common { get; set; } = new();

    public ZhipuAiSummaryConfig ZhipuAi { get; set; } = new();

    public ChatGptAiSummaryConfig ChatGpt { get; set; } = new();

    public DeepSeekAiSummaryConfig DeepSeek { get; set; } = new();

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    [System.Text.Json.Serialization.JsonPropertyName("proxy")]
    public AiSummaryProxyConfig? LegacyProxy { get; set; }

    public bool MigrateLegacyCommonConfig()
    {
        var migrated = false;
        if (ZhipuAi.HasLegacyCommonConfig)
        {
            Common.SystemPrompt = ZhipuAi.LegacySystemPrompt ?? Common.SystemPrompt;
            Common.SummaryPrompt = ZhipuAi.LegacySummaryPrompt ?? Common.SummaryPrompt;
            Common.Temperature = ZhipuAi.LegacyTemperature ?? Common.Temperature;
            Common.TimeoutSeconds = ZhipuAi.LegacyTimeoutSeconds ?? Common.TimeoutSeconds;
            Common.RequestRetryCount = ZhipuAi.LegacyRequestRetryCount ?? Common.RequestRetryCount;
            Common.Stream = ZhipuAi.LegacyStream ?? Common.Stream;
            ZhipuAi.ClearLegacyCommonConfig();
            migrated = true;
        }

        if (LegacyProxy is not null)
        {
            CopyLegacyProxyIfNeeded(ZhipuAi);
            CopyLegacyProxyIfNeeded(ChatGpt);
            CopyLegacyProxyIfNeeded(DeepSeek);
            LegacyProxy = null;
            migrated = true;
        }

        return migrated;
    }

    private void CopyLegacyProxyIfNeeded(AiSummaryProviderConfig providerConfig)
    {
        if (HasProxySettings(providerConfig.Proxy))
            return;

        providerConfig.Proxy = CloneProxy(LegacyProxy!);
    }

    private static bool HasProxySettings(AiSummaryProxyConfig? proxy)
    {
        return proxy is not null &&
               (proxy.Enabled ||
                !string.IsNullOrWhiteSpace(proxy.Url) ||
                !string.IsNullOrWhiteSpace(proxy.Username) ||
                !string.IsNullOrWhiteSpace(proxy.Password));
    }

    private static AiSummaryProxyConfig CloneProxy(AiSummaryProxyConfig proxy)
    {
        return new AiSummaryProxyConfig
        {
            Enabled = proxy.Enabled,
            Url = proxy.Url ?? string.Empty,
            Username = proxy.Username ?? string.Empty,
            Password = proxy.Password ?? string.Empty
        };
    }
}

