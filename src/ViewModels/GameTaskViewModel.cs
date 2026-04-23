using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Game_Daily_Routine_Launcher;

public partial class GameTaskViewModel : ObservableObject
{
    private readonly GameTaskConfig _config;

    public string Id => _config.Id;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _toolPath;

    [ObservableProperty]
    private string _toolArgs;

    [ObservableProperty]
    private string _gameProcessName;

    [ObservableProperty]
    private bool _enabled;

    [ObservableProperty]
    private LaunchMode _launchMode;

    [ObservableProperty]
    private int _delayBeforeStartMs;

    [ObservableProperty]
    private TaskState _state = TaskState.Idle;

    [ObservableProperty]
    private bool _isConfigExpanded;

    public string StateText => State switch
    {
        TaskState.Idle => "等待中",
        TaskState.Launching => "启动中",
        TaskState.Running => "运行中",
        TaskState.Monitoring => "监控中",
        TaskState.Completed => "已完成",
        TaskState.Error => "出错",
        _ => "未知"
    };

    public GameTaskViewModel(GameTaskConfig config)
    {
        _config = config;
        _name = config.Name;
        _toolPath = config.ToolPath;
        _toolArgs = config.ToolArgs;
        _gameProcessName = config.GameProcessName;
        _enabled = config.Enabled;
        _launchMode = config.LaunchMode;
        _delayBeforeStartMs = config.DelayBeforeStartMs;
    }

    partial void OnStateChanged(TaskState value)
    {
        OnPropertyChanged(nameof(StateText));
    }

    [RelayCommand]
    private void ToggleConfig()
    {
        IsConfigExpanded = !IsConfigExpanded;
    }

    [RelayCommand]
    private void BrowseToolPath()
    {
        var dialog = new OpenFileDialog
        {
            Title = $"选择 {Name} 的工具路径",
            Filter = "可执行文件 (*.exe)|*.exe|所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            ToolPath = dialog.FileName;
        }
    }

    public GameTaskConfig ToConfig()
    {
        _config.Name = Name;
        _config.ToolPath = ToolPath;
        _config.ToolArgs = ToolArgs;
        _config.GameProcessName = GameProcessName;
        _config.Enabled = Enabled;
        _config.LaunchMode = LaunchMode;
        _config.DelayBeforeStartMs = DelayBeforeStartMs;
        return _config;
    }
}
