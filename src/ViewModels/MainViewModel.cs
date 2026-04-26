using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Game_Daily_Routine_Launcher;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly AudioService _audioService;
    private TaskChainRunner? _chainRunner;
    private AppConfig _appConfig;

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
    private bool _shutdownOnComplete;

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

    public MainViewModel()
    {
        _configService = new ConfigService();
        _audioService = new AudioService();
        _appConfig = _configService.Load();

        LoadConfig();
    }

    private void LoadConfig()
    {
        Tasks.Clear();
        foreach (var taskConfig in _appConfig.Tasks)
            Tasks.Add(new GameTaskViewModel(taskConfig));

        MuteOnStart = _appConfig.MuteOnStart;
        ShutdownOnComplete = _appConfig.ShutdownOnComplete;
        ScheduledHour = _appConfig.ScheduledHour;
        ScheduledMinute = _appConfig.ScheduledMinute;
        ScheduledEndHour = _appConfig.ScheduledEndHour;
        ScheduledEndMinute = _appConfig.ScheduledEndMinute;
        PollIntervalSeconds = _appConfig.PollIntervalSeconds;
        AutoRunOnStart = SystemService.IsAutoRunRegistered();
        IsMuted = _audioService.IsMuted;
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
        AddLog(IsMuted ? "系统音量已静音" : "系统音量已恢复");
    }

    [RelayCommand]
    private void SaveConfig()
    {
        _appConfig.Tasks = Tasks.Select(t => t.ToConfig()).ToList();
        _appConfig.MuteOnStart = MuteOnStart;
        _appConfig.ShutdownOnComplete = ShutdownOnComplete;
        _appConfig.ScheduledHour = ScheduledHour;
        _appConfig.ScheduledMinute = ScheduledMinute;
        _appConfig.ScheduledEndHour = ScheduledEndHour;
        _appConfig.ScheduledEndMinute = ScheduledEndMinute;
        _appConfig.PollIntervalSeconds = PollIntervalSeconds;
        _configService.Save(_appConfig);
        AddLog("配置已保存");
    }

    partial void OnAutoRunOnStartChanged(bool value)
    {
        if (value)
            SystemService.RegisterAutoRun();
        else
            SystemService.UnregisterAutoRun();
    }

    private void AddLog(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogEntries.Add(entry);
    }
}
