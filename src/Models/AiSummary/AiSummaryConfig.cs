namespace Game_Daily_Routine_Launcher;

public class AiSummaryConfig
{
    public AiSummaryProviderType Provider { get; set; } = AiSummaryProviderType.Off;

    public AiSummaryCommonConfig Common { get; set; } = new();

    public ZhipuAiSummaryConfig ZhipuAi { get; set; } = new();

    public ChatGptAiSummaryConfig ChatGpt { get; set; } = new();

    public DeepSeekAiSummaryConfig DeepSeek { get; set; } = new();

    public bool MigrateLegacyCommonConfig()
    {
        if (!ZhipuAi.HasLegacyCommonConfig)
            return false;

        Common.SystemPrompt = ZhipuAi.LegacySystemPrompt ?? Common.SystemPrompt;
        Common.SummaryPrompt = ZhipuAi.LegacySummaryPrompt ?? Common.SummaryPrompt;
        Common.Temperature = ZhipuAi.LegacyTemperature ?? Common.Temperature;
        Common.TimeoutSeconds = ZhipuAi.LegacyTimeoutSeconds ?? Common.TimeoutSeconds;
        Common.RequestRetryCount = ZhipuAi.LegacyRequestRetryCount ?? Common.RequestRetryCount;
        Common.Stream = ZhipuAi.LegacyStream ?? Common.Stream;
        ZhipuAi.ClearLegacyCommonConfig();
        return true;
    }
}

