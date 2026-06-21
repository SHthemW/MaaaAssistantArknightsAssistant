namespace Game_Daily_Routine_Launcher;

public class AiSummaryConfig
{
    public AiSummaryProviderType Provider { get; set; } = AiSummaryProviderType.Off;

    public ZhipuAiSummaryConfig ZhipuAi { get; set; } = new();
}

public class ZhipuAiSummaryConfig
{
    public string ApiKey { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = "https://open.bigmodel.cn/api/paas/v4/chat/completions";

    public string Model { get; set; } = "glm-4.7-flash";

    public string SystemPrompt { get; set; } = "你是一个有用的AI助手。";

    public double Temperature { get; set; } = 1.0;

    public bool Stream { get; set; } = true;
}

