namespace Game_Daily_Routine_Launcher;

public class AiSummaryConfig
{
    public AiSummaryProviderType Provider { get; set; } = AiSummaryProviderType.Off;

    public ZhipuAiSummaryConfig ZhipuAi { get; set; } = new();
}

public class ZhipuAiSummaryConfig
{
    public const string DefaultSummaryPrompt = "你需要根据下面的运行日志，简单总结每条任务完成情况。\n请使用简洁中文输出，逐条列出任务结论。每条任务须分析任务内容, 说明是完美完成还是有未完成的项.";

    public string ApiKey { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = "https://open.bigmodel.cn/api/paas/v4/chat/completions";

    public string Model { get; set; } = "glm-4.7-flash";

    public string SystemPrompt { get; set; } = "你是一个有用的AI助手。";

    public string SummaryPrompt { get; set; } = DefaultSummaryPrompt;

    public double Temperature { get; set; } = 1.0;

    public int TimeoutSeconds { get; set; } = 800;

    public int RequestRetryCount { get; set; } = 3;

    public bool ThinkingEnabled { get; set; } = true;

    public bool Stream { get; set; } = true;
}

