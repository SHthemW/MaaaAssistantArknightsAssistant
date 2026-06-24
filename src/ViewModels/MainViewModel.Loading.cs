namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel
{
    private void LoadConfig()
    {
        _isLoading = true;

        Tasks.Clear();
        foreach (var taskConfig in _appConfig.Tasks)
        {
            var vm = new GameTaskViewModel(taskConfig) { ConfigChanged = SaveConfig };
            Tasks.Add(vm);
        }

        LoadBasicConfig();
        LoadAiSummaryConfig();
        LoadWebhookConfig();

        AutoRunOnStart = SystemService.IsAutoRunRegistered();
        IsMuted = _audioService.IsMuted;
        LoadWebhookPushContentOptions();
        RefreshTwinkleTrayAvailability();

        _isLoading = false;

        if (App.IsAutoRun && RandomStartEnabled)
            _randomAutoStartTime = _appConfig.GetRandomScheduledStartTime(DateTime.Now, Random.Shared);

        if (App.IsAutoRun && ShouldStartForAutoRun(DateTime.Now))
            QueueAutoStart();

        if (MuteOnStart && (!MuteOnlyOnAutoRun || App.IsAutoRun))
            StartStartupMuteEnforcement();

        if (TwinkleTrayOnStart && (!TwinkleTrayOnlyOnAutoRun || App.IsAutoRun) && TwinkleTrayIsAvailable)
            StartStartupTwinkleTrayEnforcement();

        RefreshWebhookRelayState();

        _scheduleTimer.Interval = TimeSpan.FromSeconds(Math.Max(PollIntervalSeconds, 1));
        _scheduleTimer.Start();
        IsSchedulePolling = true;
    }

    private void LoadBasicConfig()
    {
        MuteOnStart = _appConfig.MuteOnStart;
        MuteOnlyOnAutoRun = _appConfig.MuteOnlyOnAutoRun;
        ShutdownOnComplete = _appConfig.ShutdownOnComplete;
        RandomStartEnabled = _appConfig.RandomStartEnabled;
        ScheduledHour = _appConfig.ScheduledHour;
        ScheduledMinute = _appConfig.ScheduledMinute;
        ScheduledEndHour = _appConfig.ScheduledEndHour;
        ScheduledEndMinute = _appConfig.ScheduledEndMinute;
        PollIntervalSeconds = _appConfig.PollIntervalSeconds;
        TwinkleTrayOnStart = _appConfig.TwinkleTrayOnStart;
        TwinkleTrayOnlyOnAutoRun = _appConfig.TwinkleTrayOnlyOnAutoRun;
        ShutdownOnlyOnAutoRun = _appConfig.ShutdownOnlyOnAutoRun;
    }

    private void LoadAiSummaryConfig()
    {
        AiSummaryEnabled = _appConfig.AiSummary.Provider != AiSummaryProviderType.Off;
        AiSummaryOnlyOnAutoRun = _appConfig.AiSummaryOnlyOnAutoRun;
        AiSummaryExpanded = _appConfig.AiSummaryExpanded;
        SelectedAiSummaryProvider = _appConfig.AiSummary.Provider == AiSummaryProviderType.Off
            ? AiSummaryProviderType.ZhipuAi
            : _appConfig.AiSummary.Provider;
        ZhipuApiKey = _appConfig.AiSummary.ZhipuAi.ApiKey;
        ZhipuApiUrl = _appConfig.AiSummary.ZhipuAi.ApiUrl;
        ZhipuModel = _appConfig.AiSummary.ZhipuAi.Model;
        ZhipuSystemPrompt = _appConfig.AiSummary.ZhipuAi.SystemPrompt;
        ZhipuSummaryPrompt = string.IsNullOrWhiteSpace(_appConfig.AiSummary.ZhipuAi.SummaryPrompt)
            ? ZhipuAiSummaryConfig.DefaultSummaryPrompt
            : _appConfig.AiSummary.ZhipuAi.SummaryPrompt;
        ZhipuTemperature = _appConfig.AiSummary.ZhipuAi.Temperature;
        ZhipuTimeoutSeconds = _appConfig.AiSummary.ZhipuAi.TimeoutSeconds;
        ZhipuRequestRetryCount = _appConfig.AiSummary.ZhipuAi.RequestRetryCount;
        ZhipuThinkingEnabled = _appConfig.AiSummary.ZhipuAi.ThinkingEnabled;
        ZhipuStream = _appConfig.AiSummary.ZhipuAi.Stream;
    }

    private void LoadWebhookConfig()
    {
        WebhookEnabled = _appConfig.WebhookEnabled;
        WebhookUrl = _appConfig.WebhookUrl;
        WebhookBody = _appConfig.WebhookBody;
        WebhookRelayEnabled = _appConfig.WebhookRelayEnabled;
        WebhookRelayPort = _appConfig.WebhookRelayPort;
        WebhookRelaySourceUrl = _appConfig.WebhookRelaySourceUrl;
        WebhookOnlyPushAiSummary = _appConfig.WebhookOnlyPushAiSummary;
        WebhookCustomPushContentEnabled = _appConfig.WebhookCustomPushContentEnabled;
        WebhookEnabledExpanded = _appConfig.WebhookEnabledExpanded;
        WebhookRelayExpanded = _appConfig.WebhookRelayExpanded;
        WebhookCustomPushContentExpanded = _appConfig.WebhookCustomPushContentExpanded;
    }
}
