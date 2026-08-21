namespace Game_Daily_Routine_Launcher;

public sealed class ChatGptAiSummaryProvider : OpenAiCompatibleAiSummaryProvider
{
    public override AiSummaryProviderType ProviderType => AiSummaryProviderType.ChatGpt;

    protected override string ProviderDisplayName => "ChatGPT";

    protected override AiSummaryProviderConfig GetProviderConfig(AiSummaryConfig config) => config.ChatGpt;
}
