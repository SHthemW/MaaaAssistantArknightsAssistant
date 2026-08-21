namespace Game_Daily_Routine_Launcher;

public sealed class DeepSeekAiSummaryProvider : OpenAiCompatibleAiSummaryProvider
{
    public override AiSummaryProviderType ProviderType => AiSummaryProviderType.DeepSeek;

    protected override string ProviderDisplayName => "DeepSeek";

    protected override AiSummaryProviderConfig GetProviderConfig(AiSummaryConfig config) => config.DeepSeek;
}
