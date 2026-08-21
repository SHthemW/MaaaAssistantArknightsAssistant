namespace Game_Daily_Routine_Launcher;

public class AiSummaryCommonConfig
{
    public const string DefaultSystemPrompt = "你是一个有用的AI助手。";
    public const string DefaultSummaryPrompt = "你需要根据下面的运行日志，简单总结每条任务完成情况。\n请使用简洁中文输出，逐条列出任务结论。每条任务须分析任务内容, 说明是完美完成还是有未完成的项.";

    public string SystemPrompt { get; set; } = DefaultSystemPrompt;

    public string SummaryPrompt { get; set; } = DefaultSummaryPrompt;

    public double Temperature { get; set; } = 1.0;

    public int TimeoutSeconds { get; set; } = 800;

    public int RequestRetryCount { get; set; } = 3;

    public bool Stream { get; set; } = true;
}
