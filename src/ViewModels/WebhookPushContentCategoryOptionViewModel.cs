using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Daily_Routine_Launcher;

public partial class WebhookPushContentCategoryOptionViewModel : ObservableObject
{
    private readonly Action? _changed;

    public WebhookPushContentCategory Category { get; }

    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isEnabled;

    public WebhookPushContentCategoryOptionViewModel(
        WebhookPushContentCategory category,
        string displayName,
        bool isEnabled,
        Action? changed)
    {
        Category = category;
        DisplayName = displayName;
        _isEnabled = isEnabled;
        _changed = changed;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        _changed?.Invoke();
    }
}
