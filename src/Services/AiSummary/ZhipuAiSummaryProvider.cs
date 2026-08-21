namespace Game_Daily_Routine_Launcher;

public sealed class ZhipuAiSummaryProvider : OpenAiCompatibleAiSummaryProvider
{
    public override AiSummaryProviderType ProviderType => AiSummaryProviderType.ZhipuAi;

    protected override string ProviderDisplayName => "智谱AI";

    protected override AiSummaryProviderConfig GetProviderConfig(AiSummaryConfig config) => config.ZhipuAi;

    protected override Dictionary<string, object> BuildPayload(AiSummaryConfig config, string prompt)
    {
        var payload = base.BuildPayload(config, prompt);
        payload["thinking"] = new
        {
            type = config.ZhipuAi.ThinkingEnabled ? "enabled" : "disabled"
        };
        return payload;
    }
}
