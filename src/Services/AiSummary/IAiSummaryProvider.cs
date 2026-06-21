namespace Game_Daily_Routine_Launcher;

public interface IAiSummaryProvider
{
    AiSummaryProviderType ProviderType { get; }

    string BuildRequestBodyJson(ZhipuAiSummaryConfig config, string prompt);

    Task<string> GenerateAsync(ZhipuAiSummaryConfig config, string prompt, CancellationToken cancellationToken = default);
}
