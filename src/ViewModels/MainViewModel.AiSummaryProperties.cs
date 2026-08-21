using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    public IReadOnlyList<AiSummaryProviderOption> AiSummaryProviderOptions { get; } =
    [
        new(AiSummaryProviderType.ZhipuAi, "智谱AI"),
        new(AiSummaryProviderType.ChatGpt, "ChatGPT"),
        new(AiSummaryProviderType.DeepSeek, "DeepSeek")
    ];

    [ObservableProperty] private bool _aiSummaryEnabled;
    [ObservableProperty] private bool _aiSummaryOnlyOnAutoRun;
    [ObservableProperty] private bool _aiSummaryExpanded = true;
    [ObservableProperty] private bool _hasRecentAiSummaryPromptLog;
    [ObservableProperty] private AiSummaryProviderType _selectedAiSummaryProvider = AiSummaryProviderType.Off;

    [ObservableProperty] private string _zhipuApiKey = string.Empty;
    [ObservableProperty] private string _zhipuApiUrl = string.Empty;
    [ObservableProperty] private string _zhipuModel = string.Empty;
    [ObservableProperty] private bool _zhipuThinkingEnabled = true;

    [ObservableProperty] private string _chatGptApiKey = string.Empty;
    [ObservableProperty] private string _chatGptApiUrl = string.Empty;
    [ObservableProperty] private string _chatGptModel = string.Empty;

    [ObservableProperty] private string _deepSeekApiKey = string.Empty;
    [ObservableProperty] private string _deepSeekApiUrl = string.Empty;
    [ObservableProperty] private string _deepSeekModel = string.Empty;

    [ObservableProperty] private string _aiSystemPrompt = string.Empty;
    [ObservableProperty] private string _aiSummaryPrompt = string.Empty;
    [ObservableProperty] private double _aiTemperature = 1.0;
    [ObservableProperty] private int _aiTimeoutSeconds = 800;
    [ObservableProperty] private int _aiRequestRetryCount = 3;
    [ObservableProperty] private bool _aiStream = true;

    public string SelectedAiApiKey
    {
        get => SelectedAiSummaryProvider switch
        {
            AiSummaryProviderType.ZhipuAi => ZhipuApiKey,
            AiSummaryProviderType.ChatGpt => ChatGptApiKey,
            AiSummaryProviderType.DeepSeek => DeepSeekApiKey,
            _ => string.Empty
        };
        set
        {
            switch (SelectedAiSummaryProvider)
            {
                case AiSummaryProviderType.ZhipuAi:
                    ZhipuApiKey = value;
                    break;
                case AiSummaryProviderType.ChatGpt:
                    ChatGptApiKey = value;
                    break;
                case AiSummaryProviderType.DeepSeek:
                    DeepSeekApiKey = value;
                    break;
            }
        }
    }

    public string SelectedAiApiUrl
    {
        get => SelectedAiSummaryProvider switch
        {
            AiSummaryProviderType.ZhipuAi => ZhipuApiUrl,
            AiSummaryProviderType.ChatGpt => ChatGptApiUrl,
            AiSummaryProviderType.DeepSeek => DeepSeekApiUrl,
            _ => string.Empty
        };
        set
        {
            switch (SelectedAiSummaryProvider)
            {
                case AiSummaryProviderType.ZhipuAi:
                    ZhipuApiUrl = value;
                    break;
                case AiSummaryProviderType.ChatGpt:
                    ChatGptApiUrl = value;
                    break;
                case AiSummaryProviderType.DeepSeek:
                    DeepSeekApiUrl = value;
                    break;
            }
        }
    }

    public string SelectedAiModel
    {
        get => SelectedAiSummaryProvider switch
        {
            AiSummaryProviderType.ZhipuAi => ZhipuModel,
            AiSummaryProviderType.ChatGpt => ChatGptModel,
            AiSummaryProviderType.DeepSeek => DeepSeekModel,
            _ => string.Empty
        };
        set
        {
            switch (SelectedAiSummaryProvider)
            {
                case AiSummaryProviderType.ZhipuAi:
                    ZhipuModel = value;
                    break;
                case AiSummaryProviderType.ChatGpt:
                    ChatGptModel = value;
                    break;
                case AiSummaryProviderType.DeepSeek:
                    DeepSeekModel = value;
                    break;
            }
        }
    }

    partial void OnSelectedAiSummaryProviderChanged(AiSummaryProviderType value)
    {
        OnPropertyChanged(nameof(SelectedAiApiKey));
        OnPropertyChanged(nameof(SelectedAiApiUrl));
        OnPropertyChanged(nameof(SelectedAiModel));
    }
}
