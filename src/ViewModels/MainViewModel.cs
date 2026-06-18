using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly AudioService _audioService;
    private readonly DispatcherTimer _scheduleTimer;
    private readonly DispatcherTimer _startupMuteTimer;
    private TaskChainRunner? _chainRunner;
    private AppConfig _appConfig;
    private bool _isLoading;
    private bool _didAutoMute;
    private bool _originalMuteState;
    private bool _hasRunInCurrentWindow;
    private DateTime? _randomAutoStartTime;
    private int _startupMuteSuccessStreak;

    public ObservableCollection<GameTaskViewModel> Tasks { get; } = [];
    public ObservableCollection<string> LogEntries { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartAllCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopAllCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private bool _muteOnStart;

    [ObservableProperty]
    private bool _muteOnlyOnAutoRun;

    [ObservableProperty]
    private bool _shutdownOnComplete;

    [ObservableProperty]
    private bool _randomStartEnabled;

    [ObservableProperty]
    private bool _autoRunOnStart;

    [ObservableProperty]
    private int _scheduledHour;

    [ObservableProperty]
    private int _scheduledMinute;

    [ObservableProperty]
    private int _scheduledEndHour;

    [ObservableProperty]
    private int _scheduledEndMinute;

    [ObservableProperty]
    private int _pollIntervalSeconds;

    [ObservableProperty]
    private bool _isSchedulePolling;

    [ObservableProperty]
    private bool _webhookEnabled;

    [ObservableProperty]
    private string _webhookUrl = string.Empty;

    [ObservableProperty]
    private string _webhookBody = string.Empty;

    public MainViewModel()
    {
        _configService = new ConfigService();
        _audioService = new AudioService();
        _appConfig = _configService.Load();

        _scheduleTimer = new DispatcherTimer();
        _scheduleTimer.Tick += OnScheduleTimerTick;
        _startupMuteTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
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
        AutoRunOnStart = SystemService.IsAutoRunRegistered();
        IsMuted = _audioService.IsMuted;

        _isLoading = false;

        if (App.IsAutoRun && RandomStartEnabled)
            _randomAutoStartTime = _appConfig.GetRandomScheduledStartTime(DateTime.Now, Random.Shared);

        var shouldStartForAutoRun = App.IsAutoRun && ShouldStartForAutoRun(DateTime.Now);
        var shouldAutoMute = MuteOnStart && (!MuteOnlyOnAutoRun || App.IsAutoRun);

        if (shouldAutoMute)
            StartStartupMuteEnforcement();

        if (shouldStartForAutoRun)
        {
            _hasRunInCurrentWindow = true;
            StartAllCommand.Execute(null);
        }

        _scheduleTimer.Interval = TimeSpan.FromSeconds(Math.Max(PollIntervalSeconds, 1));
        _scheduleTimer.Start();
        IsSchedulePolling = true;
    }

    private void OnScheduleTimerTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var inRange = IsInScheduledTimeRange(now);

        if (!inRange)
        {
            _hasRunInCurrentWindow = false;
            RefreshRandomAutoStartTimeIfNeeded(now);
            return;
        }

        if (!IsRunning && !_hasRunInCurrentWindow && ShouldStartForAutoRun(now))
        {
            _hasRunInCurrentWindow = true;
            StartAllCommand.Execute(null);
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

    private void OnStartupMuteTimerTick(object? sender, EventArgs e)
    {
        EnforceStartupMuteOnce();
    }

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

    private bool IsInScheduledTimeRange(DateTime now)
    {
        return _appConfig.IsInScheduledTimeRange(TimeOnly.FromDateTime(now));
    }

    [RelayCommand(CanExecute = nameof(CanStartAll))]
    private async Task StartAllAsync()
    {
        SaveConfig();
        IsRunning = true;

        var enabledTasks = Tasks.Where(t => t.Enabled).Select(t => t.ToConfig()).ToList();

        if (enabledTasks.Count == 0)
        {
            AddLog("没有启用的任务");
            IsRunning = false;
            return;
        }

        foreach (var t in Tasks) t.State = TaskState.Idle;

        _chainRunner = new TaskChainRunner(_audioService, PollIntervalSeconds * 1000);

        _chainRunner.LogMessage += msg => Application.Current.Dispatcher.Invoke(() => AddLog(msg));
        _chainRunner.TaskStateChanged += (id, state) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var vm = Tasks.FirstOrDefault(t => t.Id == id);
                if (vm != null && Enum.TryParse<TaskState>(state, out var s))
                    vm.State = s;
            });
        };
        _chainRunner.ChainCompleted += () =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsRunning = false;
                AddLog("任务链执行完毕");
            });
        };

        AddLog("开始执行任务链...");

        await Task.Run(() => _chainRunner.RunChainAsync(enabledTasks, MuteOnStart, ShutdownOnComplete));
    }

    private bool CanStartAll() => !IsRunning;

    [RelayCommand(CanExecute = nameof(CanStopAll))]
    private void StopAll()
    {
        _chainRunner?.Stop();

        foreach (var t in Tasks)
            t.State = TaskState.Idle;

        AddLog("正在停止所有任务...");
    }

    private bool CanStopAll() => IsRunning;

    [RelayCommand]
    private async Task StartSingleAsync(GameTaskViewModel? taskVm)
    {
        if (taskVm == null) return;

        SaveConfig();
        var config = taskVm.ToConfig();

        taskVm.State = TaskState.Running;
        AddLog($"单独启动 {config.Name}...");

        var runner = new TaskChainRunner(_audioService, PollIntervalSeconds * 1000);
        runner.LogMessage += msg => Application.Current.Dispatcher.Invoke(() => AddLog(msg));
        runner.TaskStateChanged += (id, state) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Enum.TryParse<TaskState>(state, out var s))
                    taskVm.State = s;
            });
        };

        await Task.Run(() => runner.RunChainAsync([config], false, false));
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _audioService.SetMute(IsMuted);
        _didAutoMute = false;
        AddLog(IsMuted ? "系统音量已静音" : "系统音量已恢复");
    }

    [RelayCommand]
    private void TestWebhook()
    {
        AddLog("这是一条来自MAAA的测试消息");
    }

    private static readonly HashSet<string> NonConfigProperties =
    [
        nameof(IsRunning), nameof(IsMuted), nameof(AutoRunOnStart), nameof(IsSchedulePolling)
    ];

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (!_isLoading && !NonConfigProperties.Contains(e.PropertyName!))
            SaveConfig();
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
        _configService.Save(_appConfig);
    }

    partial void OnPollIntervalSecondsChanged(int value)
    {
        _scheduleTimer.Interval = TimeSpan.FromSeconds(Math.Max(value, 1));
    }

    partial void OnAutoRunOnStartChanged(bool value)
    {
        if (_isLoading) return;

        var (success, message) = value
            ? SystemService.RegisterAutoRun()
            : SystemService.UnregisterAutoRun();

        var action = value ? "注册" : "取消";
        AddLog(success
            ? $"开机自启{action}成功：{message}"
            : $"开机自启{action}失败：{message}");
    }

    public void Cleanup()
    {
        _scheduleTimer.Stop();
        _startupMuteTimer.Stop();
        IsSchedulePolling = false;

        if (_didAutoMute && _audioService.IsMuted)
        {
            _audioService.SetMute(_originalMuteState);
            AddLog("退出时恢复原始静音状态");
        }
    }

    private void AddLog(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        var entry = $"[{time}] {message}";
        LogEntries.Add(entry);

        if (WebhookEnabled && !string.IsNullOrWhiteSpace(WebhookUrl))
            WebhookService.Send(WebhookUrl, WebhookBody, time, message);
    }
}
