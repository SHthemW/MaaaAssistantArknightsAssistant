using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel : ObservableObject
{
    private static readonly HashSet<string> NonConfigProperties =
    [
        nameof(IsRunning), nameof(IsMuted), nameof(TwinkleTrayIsAvailable), nameof(TwinkleTrayAvailabilityMessage),
        nameof(AutoRunOnStart), nameof(IsSchedulePolling), nameof(WebhookRelayIsRunning), nameof(WebhookRelayStatusMessage),
        nameof(WebhookEnabledExpanded), nameof(WebhookRelayExpanded), nameof(WebhookCustomPushContentExpanded), nameof(AiSummaryExpanded)
    ];

    private readonly ConfigService _configService;
    private readonly AudioService _audioService;
    private readonly AiSummaryService _aiSummaryService;
    private readonly AiPromptLogService _aiPromptLogService;
    private readonly TwinkleTrayService _twinkleTrayService;
    private readonly WebhookRelayService _webhookRelayService;
    private readonly DispatcherTimer _scheduleTimer;
    private readonly DispatcherTimer _startupMuteTimer;
    private readonly DispatcherTimer _startupTwinkleTrayTimer;
    private AppConfig _appConfig;
    private TaskChainRunner? _chainRunner;
    private bool _isLoading;
    private bool _didAutoMute;
    private bool _originalMuteState;
    private bool _didDimTwinkleTray;
    private IReadOnlyList<TwinkleTrayMonitorState> _originalTwinkleTrayStates = Array.Empty<TwinkleTrayMonitorState>();
    private bool _hasRunInCurrentWindow;
    private bool _autoSummaryRequested;
    private DateTime? _randomAutoStartTime;
    private int _startupMuteSuccessStreak;
    private int _startupTwinkleTraySuccessStreak;
    private bool _twinkleTrayStartupInProgress;

    public ObservableCollection<GameTaskViewModel> Tasks { get; } = [];
    public ObservableCollection<LogEntryRecord> LogEntries { get; } = [];
    public IReadOnlyList<AiSummaryProviderOption> AiSummaryProviderOptions { get; } =
    [
        new(AiSummaryProviderType.ZhipuAi, "智谱AI")
    ];
    public ObservableCollection<WebhookPushContentCategoryOptionViewModel> WebhookPushContentOptions { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopAllCommand))]
    private bool _isRunning;

    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _muteOnStart;
    [ObservableProperty] private bool _muteOnlyOnAutoRun;
    [ObservableProperty] private bool _shutdownOnComplete;
    [ObservableProperty] private bool _randomStartEnabled;
    [ObservableProperty] private bool _autoRunOnStart;
    [ObservableProperty] private int _scheduledHour;
    [ObservableProperty] private int _scheduledMinute;
    [ObservableProperty] private int _scheduledEndHour;
    [ObservableProperty] private int _scheduledEndMinute;
    [ObservableProperty] private int _pollIntervalSeconds;
    [ObservableProperty] private bool _isSchedulePolling;
    [ObservableProperty] private bool _webhookEnabled;
    [ObservableProperty] private string _webhookUrl = string.Empty;
    [ObservableProperty] private string _webhookBody = string.Empty;
    [ObservableProperty] private bool _webhookRelayEnabled;
    [ObservableProperty] private int _webhookRelayPort = 5058;
    [ObservableProperty] private bool _webhookOnlyPushAiSummary;
    [ObservableProperty] private bool _webhookCustomPushContentEnabled;
    [ObservableProperty] private bool _webhookCustomPushContentExpanded = true;
    [ObservableProperty] private bool _webhookRelayIsRunning;
    [ObservableProperty] private string _webhookRelayStatusMessage = string.Empty;
    [ObservableProperty] private bool _webhookEnabledExpanded = true;
    [ObservableProperty] private bool _webhookRelayExpanded = true;
    [ObservableProperty] private bool _twinkleTrayOnStart;
    [ObservableProperty] private bool _twinkleTrayOnlyOnAutoRun;
    [ObservableProperty] private bool _twinkleTrayIsAvailable;
    [ObservableProperty] private string _twinkleTrayAvailabilityMessage = string.Empty;
    [ObservableProperty] private bool _shutdownOnlyOnAutoRun;
    [ObservableProperty] private bool _aiSummaryEnabled;
    [ObservableProperty] private bool _aiSummaryOnlyOnAutoRun;
    [ObservableProperty] private bool _aiSummaryExpanded = true;
    [ObservableProperty] private AiSummaryProviderType _selectedAiSummaryProvider = AiSummaryProviderType.Off;
    [ObservableProperty] private string _zhipuApiKey = string.Empty;
    [ObservableProperty] private string _zhipuApiUrl = string.Empty;
    [ObservableProperty] private string _zhipuModel = string.Empty;
    [ObservableProperty] private string _zhipuSystemPrompt = string.Empty;
    [ObservableProperty] private double _zhipuTemperature = 1.0;
    [ObservableProperty] private bool _zhipuStream = true;

    public MainViewModel()
    {
        _configService = new ConfigService();
        _audioService = new AudioService();
        _aiSummaryService = new AiSummaryService();
        _aiPromptLogService = new AiPromptLogService();
        _twinkleTrayService = new TwinkleTrayService();
        _webhookRelayService = new WebhookRelayService();
        _appConfig = _configService.Load();

        _scheduleTimer = new DispatcherTimer();
        _scheduleTimer.Tick += OnScheduleTimerTick;

        _startupMuteTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _startupMuteTimer.Tick += OnStartupMuteTimerTick;

        _startupTwinkleTrayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _startupTwinkleTrayTimer.Tick += OnStartupTwinkleTrayTimerTick;

        LoadConfig();
    }

    private void LoadConfig()
    {
        _isLoading = true;

        Tasks.Clear();
        foreach (var taskConfig in _appConfig.Tasks)
        {
            var vm = new GameTaskViewModel(taskConfig) { ConfigChanged = SaveConfig };
            Tasks.Add(vm);
        }

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
        AiSummaryEnabled = _appConfig.AiSummary.Provider != AiSummaryProviderType.Off;
        AiSummaryOnlyOnAutoRun = _appConfig.AiSummaryOnlyOnAutoRun;
        WebhookEnabled = _appConfig.WebhookEnabled;
        WebhookUrl = _appConfig.WebhookUrl;
        WebhookBody = _appConfig.WebhookBody;
        WebhookRelayEnabled = _appConfig.WebhookRelayEnabled;
        WebhookRelayPort = _appConfig.WebhookRelayPort;
        WebhookOnlyPushAiSummary = _appConfig.WebhookOnlyPushAiSummary;
        WebhookCustomPushContentEnabled = _appConfig.WebhookCustomPushContentEnabled;
        SelectedAiSummaryProvider = _appConfig.AiSummary.Provider == AiSummaryProviderType.Off
            ? AiSummaryProviderType.ZhipuAi
            : _appConfig.AiSummary.Provider;
        ZhipuApiKey = _appConfig.AiSummary.ZhipuAi.ApiKey;
        ZhipuApiUrl = _appConfig.AiSummary.ZhipuAi.ApiUrl;
        ZhipuModel = _appConfig.AiSummary.ZhipuAi.Model;
        ZhipuSystemPrompt = _appConfig.AiSummary.ZhipuAi.SystemPrompt;
        ZhipuTemperature = _appConfig.AiSummary.ZhipuAi.Temperature;
        ZhipuStream = _appConfig.AiSummary.ZhipuAi.Stream;
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

    private void QueueAutoStart()
    {
        _autoSummaryRequested = true;
        StartAllCommand.Execute(null);
    }

    private void OnScheduleTimerTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;

        if (!IsInScheduledTimeRange(now))
        {
            _hasRunInCurrentWindow = false;
            RefreshRandomAutoStartTimeIfNeeded(now);
            return;
        }

        if (!IsRunning && !_hasRunInCurrentWindow && ShouldStartForAutoRun(now))
        {
            _hasRunInCurrentWindow = true;
            QueueAutoStart();
        }
    }

    private bool ShouldStartForAutoRun(DateTime now)
    {
        if (!IsInScheduledTimeRange(now))
            return false;

        return !RandomStartEnabled || _randomAutoStartTime is null || now >= _randomAutoStartTime.Value;
    }

    private void RefreshRandomAutoStartTimeIfNeeded(DateTime now)
    {
        if (!App.IsAutoRun || !RandomStartEnabled)
            return;

        if (_randomAutoStartTime is null || now >= _randomAutoStartTime.Value)
            _randomAutoStartTime = _appConfig.GetRandomScheduledStartTime(now, Random.Shared);
    }

    private void StartStartupMuteEnforcement()
    {
        _originalMuteState = _audioService.IsMuted;
        _didAutoMute = true;
        _startupMuteSuccessStreak = 0;

        _startupMuteTimer.Start();
        EnforceStartupMuteOnce();
    }

    private void StartStartupTwinkleTrayEnforcement()
    {
        _originalTwinkleTrayStates = _twinkleTrayService.CaptureCurrentStates();
        _didDimTwinkleTray = true;
        _startupTwinkleTraySuccessStreak = 0;

        _startupTwinkleTrayTimer.Start();
        _ = EnforceStartupTwinkleTrayAsync();
    }

    private void OnStartupMuteTimerTick(object? sender, EventArgs e) => EnforceStartupMuteOnce();

    private void OnStartupTwinkleTrayTimerTick(object? sender, EventArgs e) => _ = EnforceStartupTwinkleTrayAsync();

    private void EnforceStartupMuteOnce()
    {
        if (_audioService.IsMuted)
        {
            IsMuted = true;
            if (++_startupMuteSuccessStreak >= 3)
                _startupMuteTimer.Stop();

            return;
        }

        _audioService.SetMute(true);
        IsMuted = _audioService.IsMuted;

        if (IsMuted)
        {
            if (++_startupMuteSuccessStreak >= 3)
                _startupMuteTimer.Stop();
        }
        else
        {
            _startupMuteSuccessStreak = 0;
        }
    }

    private async Task EnforceStartupTwinkleTrayAsync()
    {
        if (!TwinkleTrayIsAvailable || _twinkleTrayStartupInProgress)
            return;

        _twinkleTrayStartupInProgress = true;
        try
        {
            var result = await Task.Run(() => _twinkleTrayService.DetectAvailability());
            if (!result.IsAvailable)
            {
                TwinkleTrayIsAvailable = false;
                TwinkleTrayAvailabilityMessage = result.Message;
                _startupTwinkleTrayTimer.Stop();
                return;
            }

            if (_originalTwinkleTrayStates.Count == 0)
                _originalTwinkleTrayStates = await Task.Run(() => _twinkleTrayService.CaptureCurrentStates());

            var dimResult = await _twinkleTrayService.SetAllLowestAsync(result);
            if (dimResult.Success)
            {
                if (++_startupTwinkleTraySuccessStreak >= 3)
                    _startupTwinkleTrayTimer.Stop();
            }
            else
            {
                _startupTwinkleTraySuccessStreak = 0;
                TwinkleTrayAvailabilityMessage = dimResult.Message;
            }
        }
        finally
        {
            _twinkleTrayStartupInProgress = false;
        }
    }

    private bool IsInScheduledTimeRange(DateTime now) => _appConfig.IsInScheduledTimeRange(TimeOnly.FromDateTime(now));

    private void ExecuteOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    private void SaveConfig()
    {
        _appConfig.Tasks = Tasks.Select(t => t.ToConfig()).ToList();
        _appConfig.MuteOnStart = MuteOnStart;
        _appConfig.MuteOnlyOnAutoRun = MuteOnlyOnAutoRun;
        _appConfig.ShutdownOnComplete = ShutdownOnComplete;
        _appConfig.RandomStartEnabled = RandomStartEnabled;
        _appConfig.ScheduledHour = ScheduledHour;
        _appConfig.ScheduledMinute = ScheduledMinute;
        _appConfig.ScheduledEndHour = ScheduledEndHour;
        _appConfig.ScheduledEndMinute = ScheduledEndMinute;
        _appConfig.PollIntervalSeconds = PollIntervalSeconds;
        _appConfig.TwinkleTrayOnStart = TwinkleTrayOnStart;
        _appConfig.TwinkleTrayOnlyOnAutoRun = TwinkleTrayOnlyOnAutoRun;
        _appConfig.ShutdownOnlyOnAutoRun = ShutdownOnlyOnAutoRun;
        _appConfig.WebhookEnabled = WebhookEnabled;
        _appConfig.WebhookUrl = WebhookUrl;
        _appConfig.WebhookBody = WebhookBody;
        _appConfig.WebhookRelayEnabled = WebhookRelayEnabled;
        _appConfig.WebhookRelayPort = WebhookRelayPort;
        _appConfig.WebhookOnlyPushAiSummary = WebhookOnlyPushAiSummary;
        _appConfig.WebhookCustomPushContentEnabled = WebhookCustomPushContentEnabled;
        _appConfig.WebhookPushCategories = WebhookPushContentOptions
            .Where(x => x.IsEnabled)
            .Select(x => x.Category)
            .ToList();
        _appConfig.AiSummaryOnlyOnAutoRun = AiSummaryOnlyOnAutoRun;
        _appConfig.AiSummary = BuildAiSummaryConfig();
        _configService.Save(_appConfig);
    }

    partial void OnPollIntervalSecondsChanged(int value)
    {
        _scheduleTimer.Interval = TimeSpan.FromSeconds(Math.Max(value, 1));
    }

    partial void OnAutoRunOnStartChanged(bool value)
    {
        if (_isLoading)
            return;

        var (success, message) = value
            ? SystemService.RegisterAutoRun()
            : SystemService.UnregisterAutoRun();

        AddLog(success
            ? $"开机自启{(value ? "注册" : "取消")}成功：{message}"
            : $"开机自启{(value ? "注册" : "取消")}失败：{message}");
    }

    public void Cleanup()
    {
        _scheduleTimer.Stop();
        _startupMuteTimer.Stop();
        _startupTwinkleTrayTimer.Stop();
        IsSchedulePolling = false;
        _ = _webhookRelayService.StopAsync();

        if (_didAutoMute && _audioService.IsMuted)
        {
            _audioService.SetMute(_originalMuteState);
            AddLog("退出时已恢复原始静音状态。");
        }

        if (_didDimTwinkleTray && TwinkleTrayIsAvailable && _originalTwinkleTrayStates.Count > 0)
        {
            var result = _twinkleTrayService.DetectAvailability();
            if (result.IsAvailable)
                _ = _twinkleTrayService.RestoreAsync(result, _originalTwinkleTrayStates);
        }
    }

    private void AddLog(string message, string? rawBody = null)
    {
        var entry = new LogEntryRecord(DateTime.Now, message)
        {
            RawBody = rawBody,
            Category = InferWebhookPushCategory(message, rawBody)
        };
        ExecuteOnUiThread(() => AppendLog(entry));
    }

    private static WebhookPushContentCategory InferWebhookPushCategory(string message, string? rawBody)
    {
        if (message.Contains("AI总结：", StringComparison.Ordinal) || message.Contains("AI 智能总结", StringComparison.Ordinal))
            return WebhookPushContentCategory.AiSummary;

        if (message.Contains("Webhook", StringComparison.Ordinal) || message.Contains("中转", StringComparison.Ordinal))
            return WebhookPushContentCategory.Webhook;

        if (message.Contains("静音", StringComparison.Ordinal) || message.Contains("音量", StringComparison.Ordinal))
            return WebhookPushContentCategory.Audio;

        if (message.Contains("亮度", StringComparison.Ordinal) || message.Contains("Twinkle Tray", StringComparison.Ordinal))
            return WebhookPushContentCategory.Brightness;

        if (message.Contains("开机自启", StringComparison.Ordinal) || message.Contains("关机", StringComparison.Ordinal) || message.Contains("设置", StringComparison.Ordinal))
            return WebhookPushContentCategory.System;

        if (message.Contains("任务链", StringComparison.Ordinal) || message.Contains("任务", StringComparison.Ordinal) || message.Contains("启动", StringComparison.Ordinal) || message.Contains("完成", StringComparison.Ordinal) || message.Contains("停止", StringComparison.Ordinal))
            return WebhookPushContentCategory.TaskExecution;

        if (message.Contains("监控", StringComparison.Ordinal) || message.Contains("等待", StringComparison.Ordinal) || message.Contains("超时", StringComparison.Ordinal) || message.Contains("进程", StringComparison.Ordinal))
            return WebhookPushContentCategory.TaskMonitoring;

        return WebhookPushContentCategory.Other;
    }

    private void LoadWebhookPushContentOptions()
    {
        var enabled = new HashSet<WebhookPushContentCategory>(_appConfig.WebhookPushCategories);
        WebhookPushContentOptions.Clear();

        foreach (var option in CreateDefaultWebhookPushContentOptions())
            WebhookPushContentOptions.Add(new WebhookPushContentCategoryOptionViewModel(option.Category, option.DisplayName, enabled.Count == 0 || enabled.Contains(option.Category), SaveConfig));
    }

    private static IReadOnlyList<(WebhookPushContentCategory Category, string DisplayName)> CreateDefaultWebhookPushContentOptions() =>
    [
        (WebhookPushContentCategory.TaskExecution, "任务启动/完成"),
        (WebhookPushContentCategory.TaskMonitoring, "进程监控"),
        (WebhookPushContentCategory.Audio, "静音/音量"),
        (WebhookPushContentCategory.Brightness, "亮度调节"),
        (WebhookPushContentCategory.Webhook, "Webhook 推送"),
        (WebhookPushContentCategory.AiSummary, "AI 总结"),
        (WebhookPushContentCategory.System, "系统设置"),
        (WebhookPushContentCategory.Other, "其他日志")
    ];

    private void AppendLog(LogEntryRecord entry)
    {
        LogEntries.Add(entry);
        Debug.WriteLine(entry.DisplayText);
        Console.WriteLine(entry.DisplayText);
        if (!string.IsNullOrWhiteSpace(entry.RawBody))
            Console.WriteLine(entry.RawBody);

        if (ShouldPushLogEntry(entry))
            _ = PushWebhookAsync(entry.Message, entry.RawBody, entry.Timestamp.ToString("HH:mm:ss"));
    }

    private bool ShouldPushLogEntry(LogEntryRecord entry)
    {
        if (!WebhookEnabled || string.IsNullOrWhiteSpace(WebhookUrl))
            return false;

        if (WebhookOnlyPushAiSummary)
            return entry.Message.StartsWith("AI总结：", StringComparison.Ordinal);

        if (WebhookCustomPushContentEnabled)
            return WebhookPushContentOptions.Any(x => x.IsEnabled && x.Category == entry.Category);

        return true;
    }

    private async Task PushWebhookAsync(string message, string? rawBody, string time)
    {
        var content = string.IsNullOrWhiteSpace(rawBody)
            ? message
            : $"{message}\n原始Body：\n{rawBody}";
        var result = await WebhookService.SendAsync(WebhookUrl, WebhookBody, time, content);
        if (result.Success)
            Debug.WriteLine($"Webhook sent: {result.RequestBody}");
    }

    private bool CanStartAll() => !IsRunning;

    private bool CanStopAll() => IsRunning;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_isLoading)
            return;

        var propertyName = e.PropertyName ?? string.Empty;
        if (!NonConfigProperties.Contains(propertyName))
            SaveConfig();
    }

    partial void OnTwinkleTrayOnStartChanged(bool value)
    {
        if (_isLoading)
            return;

        if (value && !TwinkleTrayIsAvailable)
        {
            TwinkleTrayOnStart = false;
            AddLog($"Twinkle Tray 不可用：{TwinkleTrayAvailabilityMessage}");
        }
    }

    private void RefreshTwinkleTrayAvailability()
    {
        var availability = _twinkleTrayService.DetectAvailability();
        TwinkleTrayIsAvailable = availability.IsAvailable;
        TwinkleTrayAvailabilityMessage = availability.Message;

        if (!availability.IsAvailable)
        {
            TwinkleTrayOnStart = false;
            TwinkleTrayOnlyOnAutoRun = false;
            if (!_isLoading)
                AddLog($"Twinkle Tray 不可用：{availability.Message}");
        }
    }

    private async void RefreshWebhookRelayState()
    {
        if (!WebhookRelayEnabled)
        {
            WebhookRelayIsRunning = false;
            WebhookRelayStatusMessage = "未启用。";
            await _webhookRelayService.StopAsync();
            return;
        }

        var result = await _webhookRelayService.StartAsync(WebhookRelayPort, (message, rawBody) =>
        {
            AddLog(message, rawBody);
            return Task.CompletedTask;
        });
        WebhookRelayIsRunning = result.Success;
        WebhookRelayStatusMessage = result.Message;
    }

    partial void OnWebhookRelayEnabledChanged(bool value)
    {
        if (_isLoading)
            return;

        RefreshWebhookRelayState();
    }

    partial void OnWebhookRelayPortChanged(int value)
    {
        if (_isLoading || !WebhookRelayEnabled)
            return;

        RefreshWebhookRelayState();
    }

    partial void OnWebhookOnlyPushAiSummaryChanged(bool value)
    {
        if (_isLoading)
            return;

        if (value && !AiSummaryEnabled)
            AddLog("未开启 AI 总结服务。");
    }

    partial void OnAiSummaryEnabledChanged(bool value)
    {
        if (_isLoading)
            return;

        if (value && SelectedAiSummaryProvider == AiSummaryProviderType.Off)
            SelectedAiSummaryProvider = AiSummaryProviderType.ZhipuAi;
        else if (!value)
            SelectedAiSummaryProvider = AiSummaryProviderType.ZhipuAi;
    }

    partial void OnAiSummaryOnlyOnAutoRunChanged(bool value)
    {
        if (_isLoading)
            return;

        if (!value && App.IsAutoRun)
            AddLog("AI 智能总结已设置为始终生效。");
    }
}

