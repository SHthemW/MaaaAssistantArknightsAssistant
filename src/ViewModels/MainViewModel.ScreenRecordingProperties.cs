using CommunityToolkit.Mvvm.ComponentModel;

namespace Game_Daily_Routine_Launcher;

public sealed record ScreenRecordingResolutionOption(
    ScreenRecordingResolution Value,
    string DisplayName);

public partial class MainViewModel
{
    public IReadOnlyList<ScreenRecordingResolutionOption> ScreenRecordingResolutionOptions { get; } =
    [
        new(ScreenRecordingResolution.Hd720p, "720p (1280x720)"),
        new(ScreenRecordingResolution.FullHd1080p, "1080p (1920x1080)"),
        new(ScreenRecordingResolution.QuadHd1440p, "1440p (2560x1440)"),
        new(ScreenRecordingResolution.UltraHd2160p, "2160p (3840x2160)")
    ];

    public IReadOnlyList<int> ScreenRecordingFramerateOptions =>
        ScreenRecordingProfile.SupportedFramerates;

    [ObservableProperty] private bool _screenRecordingEnabled;
    [ObservableProperty] private bool _screenRecordingExpanded = true;
    [ObservableProperty] private ScreenRecordingResolution _screenRecordingResolution = ScreenRecordingResolution.Hd720p;
    [ObservableProperty] private int _screenRecordingFramerate = 30;
    [ObservableProperty] private bool _screenRecordingRecordSystemAudio;

    public string ScreenRecordingBitrateText
    {
        get
        {
            var bitrate = ScreenRecordingProfile.FromConfig(new ScreenRecordingConfig
            {
                Resolution = ScreenRecordingResolution,
                Framerate = ScreenRecordingFramerate
            }).Bitrate;
            return $"{bitrate / 1_000_000d:0.#} Mbps";
        }
    }

    partial void OnScreenRecordingResolutionChanged(ScreenRecordingResolution value) =>
        OnPropertyChanged(nameof(ScreenRecordingBitrateText));

    partial void OnScreenRecordingFramerateChanged(int value) =>
        OnPropertyChanged(nameof(ScreenRecordingBitrateText));
}
