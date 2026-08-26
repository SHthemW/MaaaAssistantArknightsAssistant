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
    [ObservableProperty] private bool _zhipuProxyEnabled;
    [ObservableProperty] private string _zhipuProxyUrl = string.Empty;
    [ObservableProperty] private string _zhipuProxyUsername = string.Empty;
    [ObservableProperty] private string _zhipuProxyPassword = string.Empty;

    [ObservableProperty] private string _chatGptApiKey = string.Empty;
    [ObservableProperty] private string _chatGptApiUrl = string.Empty;
    [ObservableProperty] private string _chatGptModel = string.Empty;
    [ObservableProperty] private bool _chatGptProxyEnabled;
    [ObservableProperty] private string _chatGptProxyUrl = string.Empty;
    [ObservableProperty] private string _chatGptProxyUsername = string.Empty;
    [ObservableProperty] private string _chatGptProxyPassword = string.Empty;

    [ObservableProperty] private string _deepSeekApiKey = string.Empty;
    [ObservableProperty] private string _deepSeekApiUrl = string.Empty;
    [ObservableProperty] private string _deepSeekModel = string.Empty;
    [ObservableProperty] private bool _deepSeekProxyEnabled;
    [ObservableProperty] private string _deepSeekProxyUrl = string.Empty;
    [ObservableProperty] private string _deepSeekProxyUsername = string.Empty;
    [ObservableProperty] private string _deepSeekProxyPassword = string.Empty;

    [ObservableProperty] private string _aiSystemPrompt = string.Empty;
    [ObservableProperty] private string _aiSummaryPrompt = string.Empty;
    [ObservableProperty] private double _aiTemperature = 1.0;
    [ObservableProperty] private int _aiTimeoutSeconds = 800;
    [ObservableProperty] private int _aiRequestRetryCount = 3;
    [ObservableProperty] private bool _aiStream = true;

    partial void OnZhipuProxyEnabledChanged(bool value) =>
        OnPropertyChanged(nameof(SelectedAiProxyEnabled));

    partial void OnChatGptProxyEnabledChanged(bool value) =>
        OnPropertyChanged(nameof(SelectedAiProxyEnabled));

    partial void OnDeepSeekProxyEnabledChanged(bool value) =>
        OnPropertyChanged(nameof(SelectedAiProxyEnabled));

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

    public bool SelectedAiProxyEnabled
    {
        get => SelectedAiSummaryProvider switch
        {
            AiSummaryProviderType.ZhipuAi => ZhipuProxyEnabled,
            AiSummaryProviderType.ChatGpt => ChatGptProxyEnabled,
            AiSummaryProviderType.DeepSeek => DeepSeekProxyEnabled,
            _ => false
        };
        set
        {
            switch (SelectedAiSummaryProvider)
            {
                case AiSummaryProviderType.ZhipuAi:
                    ZhipuProxyEnabled = value;
                    break;
                case AiSummaryProviderType.ChatGpt:
                    ChatGptProxyEnabled = value;
                    break;
                case AiSummaryProviderType.DeepSeek:
                    DeepSeekProxyEnabled = value;
                    break;
            }
        }
    }

    public string SelectedAiProxyUrl
    {
        get => SelectedAiSummaryProvider switch
        {
            AiSummaryProviderType.ZhipuAi => ZhipuProxyUrl,
            AiSummaryProviderType.ChatGpt => ChatGptProxyUrl,
            AiSummaryProviderType.DeepSeek => DeepSeekProxyUrl,
            _ => string.Empty
        };
        set
        {
            switch (SelectedAiSummaryProvider)
            {
                case AiSummaryProviderType.ZhipuAi:
                    ZhipuProxyUrl = value;
                    break;
                case AiSummaryProviderType.ChatGpt:
                    ChatGptProxyUrl = value;
                    break;
                case AiSummaryProviderType.DeepSeek:
                    DeepSeekProxyUrl = value;
                    break;
            }
        }
    }

    public string SelectedAiProxyUsername
    {
        get => SelectedAiSummaryProvider switch
        {
            AiSummaryProviderType.ZhipuAi => ZhipuProxyUsername,
            AiSummaryProviderType.ChatGpt => ChatGptProxyUsername,
            AiSummaryProviderType.DeepSeek => DeepSeekProxyUsername,
            _ => string.Empty
        };
        set
        {
            switch (SelectedAiSummaryProvider)
            {
                case AiSummaryProviderType.ZhipuAi:
                    ZhipuProxyUsername = value;
                    break;
                case AiSummaryProviderType.ChatGpt:
                    ChatGptProxyUsername = value;
                    break;
                case AiSummaryProviderType.DeepSeek:
                    DeepSeekProxyUsername = value;
                    break;
            }
        }
    }

    public string SelectedAiProxyPassword
    {
        get => SelectedAiSummaryProvider switch
        {
            AiSummaryProviderType.ZhipuAi => ZhipuProxyPassword,
            AiSummaryProviderType.ChatGpt => ChatGptProxyPassword,
            AiSummaryProviderType.DeepSeek => DeepSeekProxyPassword,
            _ => string.Empty
        };
        set
        {
            switch (SelectedAiSummaryProvider)
            {
                case AiSummaryProviderType.ZhipuAi:
                    ZhipuProxyPassword = value;
                    break;
                case AiSummaryProviderType.ChatGpt:
                    ChatGptProxyPassword = value;
                    break;
                case AiSummaryProviderType.DeepSeek:
                    DeepSeekProxyPassword = value;
                    break;
            }
        }
    }

    partial void OnSelectedAiSummaryProviderChanged(AiSummaryProviderType value)
    {
        RefreshSelectedAiProviderProperties();
    }

    private void RefreshSelectedAiProviderProperties()
    {
        OnPropertyChanged(nameof(SelectedAiApiKey));
        OnPropertyChanged(nameof(SelectedAiApiUrl));
        OnPropertyChanged(nameof(SelectedAiModel));
        OnPropertyChanged(nameof(SelectedAiProxyEnabled));
        OnPropertyChanged(nameof(SelectedAiProxyUrl));
        OnPropertyChanged(nameof(SelectedAiProxyUsername));
        OnPropertyChanged(nameof(SelectedAiProxyPassword));
    }
}
