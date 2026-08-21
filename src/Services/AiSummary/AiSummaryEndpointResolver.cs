namespace Game_Daily_Routine_Launcher;

internal static class AiSummaryEndpointResolver
{
    public static string Resolve(AiSummaryProviderType providerType, string? configuredUrl)
    {
        if (providerType is not (AiSummaryProviderType.ChatGpt or AiSummaryProviderType.DeepSeek) ||
            !Uri.TryCreate(configuredUrl?.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https"))
            return configuredUrl ?? string.Empty;

        var path = uri.AbsolutePath.TrimEnd('/');
        string? resolvedPath = null;
        if (string.IsNullOrEmpty(path))
        {
            resolvedPath = providerType == AiSummaryProviderType.DeepSeek
                ? "/chat/completions"
                : "/v1/chat/completions";
        }
        else if (path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            resolvedPath = $"{path}/chat/completions";
        }

        if (resolvedPath is null)
            return configuredUrl.Trim();

        var builder = new UriBuilder(uri)
        {
            Path = resolvedPath,
            Fragment = string.Empty
        };
        return builder.Uri.AbsoluteUri;
    }
}
