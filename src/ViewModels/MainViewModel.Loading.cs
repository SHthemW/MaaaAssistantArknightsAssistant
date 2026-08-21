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

        var migration = SystemService.EnsureAutoRunUsesScheduledTask();
        AutoRunOnStart = SystemService.IsAutoRunRegistered();
        IsMuted = _audioService.IsMuted;
        RefreshRecentAiSummaryPromptLogAvailability();
        LoadWebhookPushContentOptions();
        RefreshTwinkleTrayAvailability();

        _isLoading = false;

        if (migration.changed && !string.IsNullOrWhiteSpace(migration.message))
            AddLog(migration.message);

        if (App.IsAutoRun && RandomStartEnabled)
            _randomAutoStartTime = _appConfig.GetRandomScheduledStartTime(DateTime.Now, Random.Shared);

        if (App.IsAutoRun && ShouldStartForAutoRun(DateTime.Now))
            QueueAutoStart();

        if (MuteOnStart && (!MuteOnlyOnAutoRun || App.IsAutoRun))
            StartStartupMuteEnforcement();

        if (TwinkleTrayOnStart && (!TwinkleTrayOnlyOnAutoRun || App.IsAutoRun))
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
        RestoreVolumeOnCompletion = _appConfig.RestoreVolumeOnCompletion;
        RestoreBrightnessOnCompletion = _appConfig.RestoreBrightnessOnCompletion;
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
        ZhipuThinkingEnabled = _appConfig.AiSummary.ZhipuAi.ThinkingEnabled;
        ChatGptApiKey = _appConfig.AiSummary.ChatGpt.ApiKey;
        ChatGptApiUrl = AiSummaryEndpointResolver.Resolve(
            AiSummaryProviderType.ChatGpt,
            _appConfig.AiSummary.ChatGpt.ApiUrl);
        ChatGptModel = _appConfig.AiSummary.ChatGpt.Model;
        DeepSeekApiKey = _appConfig.AiSummary.DeepSeek.ApiKey;
        DeepSeekApiUrl = AiSummaryEndpointResolver.Resolve(
            AiSummaryProviderType.DeepSeek,
            _appConfig.AiSummary.DeepSeek.ApiUrl);
        DeepSeekModel = _appConfig.AiSummary.DeepSeek.Model;
        AiSystemPrompt = _appConfig.AiSummary.Common.SystemPrompt;
        AiSummaryPrompt = string.IsNullOrWhiteSpace(_appConfig.AiSummary.Common.SummaryPrompt)
            ? AiSummaryCommonConfig.DefaultSummaryPrompt
            : _appConfig.AiSummary.Common.SummaryPrompt;
        AiTemperature = _appConfig.AiSummary.Common.Temperature;
        AiTimeoutSeconds = _appConfig.AiSummary.Common.TimeoutSeconds;
        AiRequestRetryCount = _appConfig.AiSummary.Common.RequestRetryCount;
        AiStream = _appConfig.AiSummary.Common.Stream;
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
