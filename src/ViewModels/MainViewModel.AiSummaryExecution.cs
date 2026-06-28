namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    private Task<string> GenerateAiSummaryInBackgroundAsync(AiSummaryConfig config, string prompt, int timeoutSeconds)
    {
        return Task.Run(() => GenerateAiSummaryWithRetryAsync(config, prompt, timeoutSeconds));
    }
}
