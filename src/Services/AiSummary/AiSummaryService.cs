namespace Game_Daily_Routine_Launcher;

public sealed class AiSummaryService
{
    private readonly IReadOnlyDictionary<AiSummaryProviderType, IAiSummaryProvider> _providers;

    public AiSummaryService()
    {
        _providers = new Dictionary<AiSummaryProviderType, IAiSummaryProvider>
        {
            [AiSummaryProviderType.ZhipuAi] = new ZhipuAiSummaryProvider()
        };
    }

    public bool CanGenerate(AiSummaryConfig config)
    {
        return config.Provider != AiSummaryProviderType.Off &&
               _providers.ContainsKey(config.Provider);
    }

    public async Task<string> GenerateAsync(AiSummaryConfig config, string prompt, CancellationToken cancellationToken = default)
    {
        if (config.Provider == AiSummaryProviderType.Off)
            return string.Empty;

        if (!_providers.TryGetValue(config.Provider, out var provider))
            throw new NotSupportedException($"不支持的AI总结平台：{config.Provider}");

        return await provider.GenerateAsync(config.ZhipuAi, prompt, cancellationToken);
    }

    public string BuildRequestBodyJson(AiSummaryConfig config, string prompt)
    {
        if (config.Provider == AiSummaryProviderType.Off)
            return string.Empty;

        if (!_providers.TryGetValue(config.Provider, out var provider))
            throw new NotSupportedException($"不支持的AI总结平台：{config.Provider}");

        return provider.BuildRequestBodyJson(config.ZhipuAi, prompt);
    }
}
