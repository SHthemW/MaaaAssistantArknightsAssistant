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
        nameof(IsRunning), nameof(IsMuted), nameof(AutoRunOnStart), nameof(IsSchedulePolling)
    ];

    private readonly ConfigService _configService;
    private readonly AudioService _audioService;
    private readonly AiSummaryService _aiSummaryService;
    private readonly DispatcherTimer _scheduleTimer;
    private readonly DispatcherTimer _startupMuteTimer;
    private AppConfig _appConfig;
    private TaskChainRunner? _chainRunner;
    private bool _isLoading;
    private bool _didAutoMute;
    private bool _originalMuteState;
    private bool _hasRunInCurrentWindow;
    private bool _autoSummaryRequested;
    private DateTime? _randomAutoStartTime;
    private int _startupMuteSuccessStreak;

    public ObservableCollection<GameTaskViewModel> Tasks { get; } = [];
    public ObservableCollection<LogEntryRecord> LogEntries { get; } = [];
    public IReadOnlyList<AiSummaryProviderOption> AiSummaryProviderOptions { get; } =
    [
        new(AiSummaryProviderType.Off, "关闭"),
        new(AiSummaryProviderType.ZhipuAi, "智谱AI")
    ];

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
        _appConfig = _configService.Load();

        _scheduleTimer = new DispatcherTimer();
        _scheduleTimer.Tick += OnScheduleTimerTick;

        _startupMuteTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _startupMuteTimer.Tick += OnStartupMuteTimerTick;

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
        WebhookEnabled = _appConfig.WebhookEnabled;
        WebhookUrl = _appConfig.WebhookUrl;
        WebhookBody = _appConfig.WebhookBody;
        SelectedAiSummaryProvider = _appConfig.AiSummary.Provider;
        ZhipuApiKey = _appConfig.AiSummary.ZhipuAi.ApiKey;
        ZhipuApiUrl = _appConfig.AiSummary.ZhipuAi.ApiUrl;
        ZhipuModel = _appConfig.AiSummary.ZhipuAi.Model;
        ZhipuSystemPrompt = _appConfig.AiSummary.ZhipuAi.SystemPrompt;
        ZhipuTemperature = _appConfig.AiSummary.ZhipuAi.Temperature;
        ZhipuStream = _appConfig.AiSummary.ZhipuAi.Stream;
        AutoRunOnStart = SystemService.IsAutoRunRegistered();
        IsMuted = _audioService.IsMuted;

        _isLoading = false;

        if (App.IsAutoRun && RandomStartEnabled)
            _randomAutoStartTime = _appConfig.GetRandomScheduledStartTime(DateTime.Now, Random.Shared);

        if (App.IsAutoRun && ShouldStartForAutoRun(DateTime.Now))
            QueueAutoStart();

        if (MuteOnStart && (!MuteOnlyOnAutoRun || App.IsAutoRun))
            StartStartupMuteEnforcement();

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

    private void OnStartupMuteTimerTick(object? sender, EventArgs e) => EnforceStartupMuteOnce();

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
        _appConfig.WebhookEnabled = WebhookEnabled;
        _appConfig.WebhookUrl = WebhookUrl;
        _appConfig.WebhookBody = WebhookBody;
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
        IsSchedulePolling = false;

        if (_didAutoMute && _audioService.IsMuted)
        {
            _audioService.SetMute(_originalMuteState);
            AddLog("退出时已恢复原始静音状态。");
        }
    }

    private void AddLog(string message)
    {
        var entry = new LogEntryRecord(DateTime.Now, message);
        ExecuteOnUiThread(() => AppendLog(entry));
    }

    private void AppendLog(LogEntryRecord entry)
    {
        LogEntries.Add(entry);
        Debug.WriteLine(entry.DisplayText);
        Console.WriteLine(entry.DisplayText);

        if (WebhookEnabled && !string.IsNullOrWhiteSpace(WebhookUrl))
            _ = WebhookService.SendAsync(WebhookUrl, WebhookBody, entry.Timestamp.ToString("HH:mm:ss"), entry.Message);
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
}
