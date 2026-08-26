namespace Game_Daily_Routine_Launcher;

/// <summary>
/// AI 总结请求使用的 HTTP(S) 代理配置喵。
/// </summary>
public sealed class AiSummaryProxyConfig
{
    public bool Enabled { get; set; }

    public string Url { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
