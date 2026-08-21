namespace Game_Daily_Routine_Launcher;

public interface IAiSummaryProvider
{
    AiSummaryProviderType ProviderType { get; }

    string BuildRequestBodyJson(AiSummaryConfig config, string prompt);

    Task<string> GenerateAsync(AiSummaryConfig config, string prompt, CancellationToken cancellationToken = default);
}
