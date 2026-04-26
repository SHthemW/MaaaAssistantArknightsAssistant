using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Game_Daily_Routine_Launcher;

public partial class GameTaskViewModel : ObservableObject
{
    private static readonly Brush NormalBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x21, 0x21));
    private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x53, 0x50));

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

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private bool _hasValidationError;

    public Brush NameForeground => HasValidationError ? ErrorBrush : NormalBrush;

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

        Validate();
    }

    partial void OnStateChanged(TaskState value)
    {
        OnPropertyChanged(nameof(StateText));
    }

    partial void OnToolPathChanged(string value) => Validate();

    partial void OnLaunchModeChanged(LaunchMode value) => Validate();

    partial void OnHasValidationErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(NameForeground));
    }

    private void Validate()
    {
        if (LaunchMode == LaunchMode.Uri)
        {
            if (string.IsNullOrWhiteSpace(ToolPath))
                SetValidation("未配置 URL");
            else if (!Uri.TryCreate(ToolPath, UriKind.Absolute, out _))
                SetValidation($"URL 格式无效: {ToolPath}");
            else
                ClearValidation();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(ToolPath))
                SetValidation("未配置工具路径");
            else if (!File.Exists(ToolPath))
                SetValidation($"工具路径不存在: {ToolPath}");
            else
                ClearValidation();
        }
    }

    private void SetValidation(string message)
    {
        ValidationMessage = message;
        HasValidationError = true;
    }

    private void ClearValidation()
    {
        ValidationMessage = string.Empty;
        HasValidationError = false;
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
